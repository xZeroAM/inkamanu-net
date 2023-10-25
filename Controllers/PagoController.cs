using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using proyecto_inkamanu_net.Data;
using proyecto_inkamanu_net.Models;
using proyecto_inkamanu_net.Models.Entity;

namespace proyecto_inkamanu_net.Controllers
{

    public class PagoController : Controller
    {
        private readonly ILogger<PagoController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        private readonly ICarritoService _carritoService;

        public PagoController(ILogger<PagoController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext context, ICarritoService carritoService)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;

            /**/

            _carritoService = carritoService;
        }

        /// <summary>
        /// Crea una instancia de un pago con un monto especificado. Si el monto es cero, 
        /// redirige al usuario al carrito con un mensaje de error. De lo contrario, 
        /// inicializa un nuevo objeto de pago con el monto y el ID del usuario actual, 
        /// y muestra la vista correspondiente para procesar el pago.
        /// </summary>
        public IActionResult Create(Double monto)
        {

            // Validar que el monto no sea cero
            if (monto == 0)
            {
                TempData["Error"] = "El monto total es cero, no se puede proceder con el pago.";
                return RedirectToAction("Index", "Carrito");
            }

            // Obtener los ítems del carrito del usuario actual
            var itemsCarrito = _context.DataCarrito
                .Include(p => p.Producto)
                .Where(s => s.UserID.Equals(_userManager.GetUserName(User)) && s.Status.Equals("PENDIENTE"))
                .ToList();

            // Verificar el stock de cada producto
            foreach (var item in itemsCarrito)
            {
                if (item.Producto.Stock < item.Cantidad)
                {
                    TempData["Error"] = $"No hay suficiente stock para el producto {item.Producto.Nombre}.";
                    return RedirectToAction("Index", "Carrito");
                }
            }

            // Si todo está bien, proceder a la vista de pago
            Pago pago = new Pago();
            pago.UserID = _userManager.GetUserName(User);
            pago.MontoTotal = monto;
            return View(pago);
        }

        /// <summary>
        /// Procesa el pago del carrito del usuario. Inicia una transacción para asegurar la integridad de las operaciones.
        /// 1. Establece la fecha de pago y agrega el pago a la base de datos.
        /// 2. Verifica la disponibilidad de stock de los productos en el carrito.
        /// 3. Si hay suficiente stock, crea un nuevo pedido y sus detalles asociados.
        /// 4. Actualiza el stock de los productos vendidos y marca los productos en el carrito como "PROCESADO".
        /// 5. Si todo es exitoso, confirma la transacción y notifica al usuario.
        /// 6. En caso de error, revierte la transacción y muestra un mensaje de error al usuario.
        /// Este método es esencial para entender cómo manejar transacciones y operaciones
        /// </summary>

        [HttpPost]
        public async Task<IActionResult> Pagar(Pago pago)
        {
            using (var transaction = _context.Database.BeginTransaction()) // Iniciar transacción
            {
                try
                {
                    pago.PaymentDate = DateTime.UtcNow;
                    _context.Add(pago);

                    var itemsProforma = await _context.DataCarrito
                        .Include(p => p.Producto)
                        .Where(s => s.UserID.Equals(pago.UserID) && s.Status.Equals("PENDIENTE"))
                        .ToListAsync();

                    foreach (var item in itemsProforma)
                    {
                        if (item.Producto.Stock < item.Cantidad)
                        {
                            TempData["Error"] = $"No hay suficiente stock para el producto {item.Producto.Nombre}.";
                            transaction.Rollback(); // Revertir transacción
                            return RedirectToAction("Carrito"); // ir a la vista carrito
                        }
                    }

                    var descuento = await _carritoService.ObtenerDescuento(_userManager.GetUserName(User));

                    var cantidadBotellas = await _carritoService.ObtenerCantidadTotalBotellas(_userManager.GetUserName(User));

                    int primerDigito = int.Parse(pago.MontoTotal.ToString()[0].ToString());

                    string? regalo = null;

                    if (cantidadBotellas >= 12 && cantidadBotellas <= 35)
                    {
                        if (primerDigito >= 1 && primerDigito <= 4)
                        {
                            regalo = "Vaso de cerveza";
                        }
                        else if (primerDigito >= 5 && primerDigito <= 9)
                        {
                            regalo = "Destapador de la marca personalizada";
                        }
                    }

                    Pedido pedido = new Pedido
                    {
                        UserID = pago.UserID,
                        Total = pago.MontoTotal,
                        pago = pago,
                        Regalo = regalo,
                        Descuento = descuento,
                        Status = "PENDIENTE"
                    };
                    _context.Add(pedido);

                    List<DetallePedido> itemsPedido = new List<DetallePedido>();
                    foreach (var item in itemsProforma)
                    {
                        DetallePedido detallePedido = new DetallePedido
                        {
                            pedido = pedido,
                            Precio = item.Precio,
                            Producto = item.Producto,
                            Cantidad = item.Cantidad
                        };
                        itemsPedido.Add(detallePedido);

                        // Disminuir el stock del producto
                        item.Producto.Stock -= item.Cantidad;
                        _context.Update(item.Producto);
                    }

                    _context.AddRange(itemsPedido);

                    foreach (Proforma p in itemsProforma)
                    {
                        p.Status = "PROCESADO";
                    }

                    _context.UpdateRange(itemsProforma);

                    await _context.SaveChangesAsync();

                    transaction.Commit(); // Confirmar transacción

                    TempData["MessagePago"] = "El pago se ha registrado y su pedido nro " + pedido.ID + " esta en camino";
                    return View("Create");
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // Revertir transacción en caso de error
                    TempData["Error"] = "Ocurrió un error al procesar el pago. Por favor, inténtelo de nuevo.";
                    // Agregando un log de error
                    _logger.LogError(ex, "Error al procesar el pago");
                    return RedirectToAction("Error"); // me redirige a la vista de error
                }
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }

}
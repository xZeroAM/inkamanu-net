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

        public PagoController(ILogger<PagoController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Create(Double monto)
        {

            // Validar que el monto no sea cero
            if (monto == 0)
            {
                TempData["Error"] = "El monto total es cero, no se puede proceder con el pago.";
                return RedirectToAction("Index", "Carrito");
            }


            Pago pago = new Pago();
            pago.UserID = _userManager.GetUserName(User);
            pago.MontoTotal = monto;
            return View(pago);
        }

        /* [HttpPost]
         public IActionResult Pagar(Pago pago)
         {
             pago.PaymentDate = DateTime.UtcNow;
             _context.Add(pago);

             var itemsProforma = from o in _context.DataCarrito select o;
             itemsProforma = itemsProforma.
                 Include(p => p.Producto).
                 Where(s => s.UserID.Equals(pago.UserID) && s.Status.Equals("PENDIENTE"));

             Pedido pedido = new Pedido();
             pedido.UserID = pago.UserID;
             pedido.Total = pago.MontoTotal;
             pedido.pago = pago;
             pedido.Status = "PENDIENTE";
             _context.Add(pedido);

             List<DetallePedido> itemsPedido = new List<DetallePedido>();
             foreach (var item in itemsProforma.ToList())
             {
                 DetallePedido detallePedido = new DetallePedido();
                 detallePedido.pedido = pedido;
                 detallePedido.Precio = item.Precio;
                 detallePedido.Producto = item.Producto;
                 detallePedido.Cantidad = item.Cantidad;
                 itemsPedido.Add(detallePedido);

                 // Disminuir el stock del producto despues de la compra
                 item.Producto.Stock -= item.Cantidad;
                 if (item.Producto.Stock < 0) item.Producto.Stock = 0; // con esto me aseguro que el stcok no es negativo
                 _context.Update(item.Producto); // Actualizo la informacion del producto en mi base de datos 
             }


             _context.AddRange(itemsPedido);

             foreach (Proforma p in itemsProforma.ToList())
             {
                 p.Status = "PROCESADO";
             }

             _context.UpdateRange(itemsProforma);

             _context.SaveChanges();

             TempData["MessagePago"] = "El pago se ha registrado y su pedido nro " + pedido.ID + " esta en camino";
             return View("Create");
         }*/

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
                            return RedirectToAction("Carrito"); // Suponiendo que tienes una vista de carrito
                        }
                    }

                    Pedido pedido = new Pedido
                    {
                        UserID = pago.UserID,
                        Total = pago.MontoTotal,
                        pago = pago,
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
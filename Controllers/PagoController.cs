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
using System.Text;
namespace proyecto_inkamanu_net.Controllers
{

    public class PagoController : Controller
    {
        private readonly ILogger<PagoController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        private readonly ICarritoService _carritoService;

        private readonly IMyEmailSender _emailSender;

        public PagoController(ILogger<PagoController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext context, ICarritoService carritoService, IMyEmailSender emailSender)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;

            /**/

            _carritoService = carritoService;

            _emailSender = emailSender;
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
            Pedido pedido = null; // Declarar pedido fuera del bloque using para usarlo después en el correo electrónico
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
                            return RedirectToAction("Index", "Carrito"); // ir a la vista carrito
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

                    pedido = new Pedido
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

            // Si llegamos aquí, la transacción fue exitosa
            if (pedido != null) // Asegúrate de que pedido no es null
            {
                try
                {
                    // Asegúrate de que el usuario existe
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null || user.Email == null)
                    {
                        _logger.LogError($"No se pudo encontrar un usuario con el ID: {pago.UserID}");
                        TempData["Error"] = "No se pudo encontrar la información del usuario para enviar el correo electrónico.";
                        return RedirectToAction("Error"); // O manejar de otra manera
                    }

                    // Asegúrate de que el servicio de correo electrónico está disponible
                    if (_emailSender == null)
                    {
                        _logger.LogError("El servicio de envío de correo electrónico no está disponible.");
                        TempData["Error"] = "El servicio de correo electrónico no está configurado correctamente.";
                        return RedirectToAction("Error"); // O manejar de otra manera
                    }

                    // Preparar el mensaje de correo electrónico
                    // Obtener detalles del pedido


                    // Generar el contenido del correo electrónico
                    var emailSubject = "Confirmación de Pago y Detalles del Pedido";
                    var emailBody = await ExportarUnSoloPedidoEnTexto(pedido.ID);

                    // Enviar correo electrónico
                    await _emailSender.SendEmailAsync(user.Email, emailSubject, emailBody);


                    TempData["MessagePago"] = "El pago se ha registrado y su pedido nro " + pedido.ID + " esta en camino";
                }
                catch (Exception emailEx)
                {
                    // Log the email sending error
                    _logger.LogError(emailEx, "Error al enviar el correo electrónico de confirmación");
                    // Considera qué hacer si el correo no se puede enviar. ¿Notificar al usuario, reintentar, poner en cola para un nuevo intento?
                }
            }

            return View("Create");
        }

        public async Task<string> ExportarUnSoloPedidoEnTexto(int? id)
        {
            try
            {
                if (id == null)
                {
                    return $"El pedido con ID {id} no fue encontrado, por eso no se puede exportar en formato de texto.";
                }

                Pedido? pedido = await _context.DataPedido.FindAsync(id);

                if (pedido == null)
                {
                    return $"El pedido con ID {id} no fue encontrado, por eso no se puede exportar en formato de texto.";
                }

                ApplicationUser? cliente = await _context.Users.FirstOrDefaultAsync(u => u.UserName == pedido.UserID);

                if (cliente == null)
                {
                    return $"El cliente con ID {pedido.UserID} no fue encontrado en la tabla de Clientes.";
                }

                var detalles = (from detalle in _context.DataDetallePedido
                                join producto in _context.DataProducto on detalle.Producto.id equals producto.id
                                where detalle.pedido.ID == pedido.ID
                                select new DetallePedido2
                                {
                                    Cantidad = detalle.Cantidad,
                                    PrecioUnitario = detalle.Precio,
                                    NombreProducto = producto.Nombre,
                                    DescripcionProducto = producto.Descripcion,
                                    Importe = detalle.Cantidad * detalle.Precio
                                }).ToList();

                var html = ConstruirTexto(pedido, cliente, detalles);

                // Retorna la cadena HTML en lugar de generar un archivo PDF
                return html;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al exportar el pedido {id} a formato de texto");
                return $"Ocurrió un error al exportar el pedido {id} a formato de texto. Por favor, inténtelo de nuevo más tarde.";
            }
        }

        private string ConstruirTexto(Pedido pedido, ApplicationUser cliente, List<DetallePedido2> detalles)
        {
            var texto = new StringBuilder();
            // Encabezado

            texto.AppendLine($"Estimado(a) {cliente.Nombres},");
            texto.AppendLine("A continuación, le enviamos el detalle de su pedido:");
            texto.AppendLine();
            texto.AppendLine("+---------------------------------------+"); // Parte superior
            texto.AppendLine("|            InkaManu                 |"); // Nombre de la empresa
            texto.AppendLine("| La Molina, Av. la Fontana 1250, Lima |"); // Dirección
            texto.AppendLine("|      Teléfono: +51 927572267        |"); // Teléfono
            texto.AppendLine("|    Email: jesus_soria@usmp.pe       |"); // Email
            texto.AppendLine("+---------------------------------------+"); // Parte inferior

            texto.AppendLine(); // Línea en blanco

            // Información del Pedido
            texto.AppendLine("+---------------------------------------+"); // Parte superior
            texto.AppendLine($"| Información del Pedido ({pedido.ID}) |"); // Información del Pedido

            texto.AppendLine($"| Cliente: {cliente.Nombres} {cliente.ApellidoPat} {cliente.ApellidoMat} |"); // Nombre del Cliente
            texto.AppendLine($"| Email del Cliente: {cliente.Email} |"); // Email del Cliente
            texto.AppendLine($"| Factura N°:   ({pedido.ID})            |"); // Número de Factura


            texto.AppendLine($"| Fecha: {DateTime.Now:dd/MM/yyyy} |"); // Fecha en la que se le esta enviando el mensaje, la fecha actual de hoy

            texto.AppendLine($"| Estado del Pedido:  {pedido.Status} |"); // Estado del Pedido
            texto.AppendLine("+---------------------------------------+"); // Parte inferior

            texto.AppendLine();
            texto.AppendLine("Detalles del Pedido:");
            texto.AppendLine();

            // Encabezados de la tabla
            texto.AppendLine("+---------------------------------------+------------+-----------------------+--------------+-------------------------+");
            texto.AppendLine("| Producto                              | Cantidad   | Precio Unitario (S/)  | Importe (S/) | Descripción del Producto |");
            texto.AppendLine("+---------------------------------------+------------+-----------------------+--------------+-------------------------+");

            foreach (var detalle in detalles)
            {
                // Cada fila de la tabla
                texto.AppendLine($"| {detalle.NombreProducto.PadRight(37)} | {detalle.Cantidad.ToString().PadLeft(10)} | {detalle.PrecioUnitario.ToString().PadLeft(21)} | {detalle.Importe.ToString().PadLeft(12)} | {GetFirst12Words(detalle.DescripcionProducto).PadRight(27)} |");
            }

            double subtotal = detalles.Sum(d => Convert.ToDouble(d.Importe));
            double impuesto = Math.Round(subtotal * 0.18, 2);
            double total = subtotal - (pedido.Descuento ?? 0.0);

            // Totales
            texto.AppendLine("+---------------------------------------+------------+-----------------------+--------------+-------------------------+");
            texto.AppendLine($"| Subtotal                              |            |                       | S/ {subtotal.ToString().PadLeft(12)} |                         |");
            texto.AppendLine($"| Impuesto                              |            |                       | S/ {impuesto.ToString().PadLeft(12)} |                         |");
            texto.AppendLine($"| Descuento                             |            |                       | S/ {pedido.Descuento.ToString().PadLeft(12)} |                         |");
            texto.AppendLine($"| Total                                 |            |                       | S/ {total.ToString().PadLeft(12)} |                         |");
            texto.AppendLine("+---------------------------------------+------------+-----------------------+--------------+-------------------------+");
            texto.AppendLine();
            // Mensaje de agradecimiento
            texto.AppendLine("¡Gracias por su compra!");
            texto.AppendLine();
            texto.AppendLine("Saludos cordiales,");
            texto.AppendLine();
            texto.AppendLine("[La Empresa Cervezera Inkamanu]");
            texto.AppendLine();

            return texto.ToString();
        }

        private string GetFirst12Words(string text)
        {
            // Divide el texto en palabras
            string[] words = text.Split(' ');

            // Toma las primeras 12 palabras o todas si son menos de 12
            int maxWords = Math.Min(12, words.Length);

            // Une las primeras 12 palabras
            string result = string.Join(" ", words.Take(maxWords));

            return result;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }

}
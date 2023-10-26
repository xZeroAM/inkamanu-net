using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using proyecto_inkamanu_net.Data;
using proyecto_inkamanu_net.Models.DTO;
using proyecto_inkamanu_net.Models.Validator;
/*LIBRERIAS PARA LA PAGINACION DE LISTAR PRODUCTOS */
using X.PagedList;

/*LIBRERIAS PARA SUBR IMAGENES */
using Firebase.Auth;
using Firebase.Storage;
using System.Web.WebPages;

/*LIBRERIAS NECESARIAS PARA EXPORTAR */
using DinkToPdf;
using DinkToPdf.Contracts;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using OfficeOpenXml.Table;
using proyecto_inkamanu_net.Models;
using proyecto_inkamanu_net.Models.Entity;
using Microsoft.AspNetCore.Identity;
namespace proyecto_inkamanu_net.Controllers
{

    public class PedidoController : Controller
    {
        private readonly ILogger<PedidoController> _logger;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        // Objeto para la exportación
        private readonly IConverter _converter;

        public PedidoController(ILogger<PedidoController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConverter converter)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
            ModelState.Clear();


            _converter = converter; // PARA EXPORTAR
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

        public ActionResult ListaDePedidos(int? page)
        {
            int pageNumber = (page ?? 1); // Si no se especifica la página, asume la página 1
            int pageSize = 3; // maximo 3 pedidos por pagina


            pageNumber = Math.Max(pageNumber, 1);// Con esto se asegura de que pageNumber nunca sea menor que 1

            IPagedList listaPaginada = _context.DataPedido.ToPagedList(pageNumber, pageSize);

            return View("ListaDePedidos", listaPaginada);
        }


        /* metodos para exportar en pdf y excel desde aqui para abajo */
        public IActionResult ExportarPedidosEnPDF()
        {
            try
            {
                var pedidos = _context.DataPedido.ToList();
                var html = @"
            <html>
                <head>
                <meta charset='UTF-8'>
                    <style>
                        table {
                            width: 100%;
                            border-collapse: collapse;
                        }
                        th, td {
                            border: 1px solid black;
                            padding: 8px;
                            text-align: left;
                        }
                        th {
                            background-color: #f2f2f2;
                        }
                        img.logo {
                            position: absolute;
                            top: 0;
                            right: 0;
                            border-radius:50%;
                            height:3.3rem;
                            width:3.3rem;
                        }

                        h1 {
                            color: #40E0D0; /* Color celeste */
                        }
                    </style>
                </head>
                <body>
                    <img src='https://firebasestorage.googleapis.com/v0/b/proyectos-cb445.appspot.com/o/img_logo_inkamanu.jpeg?alt=media&token=3b834c39-f2ee-4555-8770-4f5a2bc88066&_gl=1*gxgr9z*_ga*MTcyOTkyMjIwMS4xNjk2NDU2NzU2*_ga_CW55HF8NVT*MTY5NjQ1Njc1NS4xLjEuMTY5NjQ1NzkyMy40OC4wLjA.' alt='Logo' width='100' class='logo'/>
                    <h1>Reporte de Pedidos</h1>
                    <table>
                        <tr>
                            <th>ID</th>
                            <th>UserID</th>
                            <th>Total (en soles)</th>
                       
                            <th>Status</th>
                        </tr>";

                foreach (var pedido in pedidos)
                {

                    html += $@"
                <tr>
                    <td>{pedido.ID}</td>
                    <td>{pedido.UserID}</td>
                    <td>{pedido.Total}</td>
                 
                    <td>{pedido.Status}</td>
   
                </tr>";
                }

                html += @"
                    </table>
                </body>
            </html>";

                var globalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                };
                var objectSettings = new ObjectSettings { HtmlContent = html };
                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = globalSettings,
                    Objects = { objectSettings }
                };
                var file = _converter.Convert(pdf);

                return File(file, "application/pdf", "Pedidos.pdf");

            }
            catch (Exception ex)
            {
                // Loguear el error para obtener más detalles
                _logger.LogError(ex, "Error al exportar Pedidos a PDF");
                // Retornar un mensaje de error al usuario
                return StatusCode(500, "Ocurrió un error al exportar los Pedidos a PDF. Por favor, inténtelo de nuevo más tarde.");
            }
        }

    
        public IActionResult ExportarPedidosEnExcel()
        {
            try
            {
                var resultados = (from p in _context.DataPedido
                                  join d in _context.DataDetallePedido on p.ID equals d.pedido.ID
                                  join pa in _context.DataPago on p.pago.Id equals pa.Id
                                  select new
                                  {
                                      IDPedido = p.ID,
                                      UserID = p.UserID,
                                      Total = p.Total,
                                      Status = p.Status,
                                      FechaDePago = pa.PaymentDate,
                                      NombreTarjeta = pa.NombreTarjeta,
                                      //Ultimos4DigitosTarjeta = pa.NumeroTarjeta.Length > 4 ? pa.NumeroTarjeta.Substring(pa.NumeroTarjeta.Length - 4) : pa.NumeroTarjeta,
                                      DigitosTarjeta = pa.NumeroTarjeta,
                                      MontoPagado = pa.MontoTotal,
                                      IDProducto = d.Producto.id,
                                      Cantidad = d.Cantidad,
                                      PrecioUnitario = d.Precio
                                  }).ToList();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Pedidos");

                // Agregando un título arriba de la tabla
                worksheet.Cells[1, 1].Value = "Reporte de Pedidos";
                worksheet.Cells[1, 1].Style.Font.Size = 20;
                worksheet.Cells[1, 1].Style.Font.Bold = true;

                // Cargar los datos en la fila 3 para dejar espacio para el título de Reporte de Pedidos
                worksheet.Cells[3, 1].LoadFromCollection(resultados, true);

                // Dar formato a la tabla Reporte de Pedidos
                var dataRange = worksheet.Cells[2, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column];
                var table = worksheet.Tables.Add(dataRange, "Pedidos");
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Light6;

                // Estilo para los encabezados de las columnas 
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Font.Bold = true;
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);

                // Ajustar el ancho de las columnas automáticamente
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Pedidos.xlsx");
            }
            catch (Exception ex)
            {
                // Loguear el error para obtener más detalles
                _logger.LogError(ex, "Error al exportar pedidos a Excel");
                // Retornar un mensaje de error al usuario
                return StatusCode(500, "Ocurrió un error al exportar los pedidos a Excel. Por favor, inténtelo de nuevo más tarde.");
            }
        }

        /* Para exportar individualmente los Pedidos */
        public async Task<ActionResult> ExportarUnSoloPedidoEnPDF(int? id)
        {
            try
            {

                if (id == null)
                {
                    return NotFound($"El pedido con ID {id} no fue encontrado, por eso no se puede exportar en PDF.");
                }

                Pedido? pedido = await _context.DataPedido.FindAsync(id);

                if (pedido == null)
                {
                    return NotFound($"El pedido con ID {id} no fue encontrado, por eso no se puede exportar en PDF.");
                }

                var html = $@"
            <html>
                <head>
                <meta charset='UTF-8'>
                    <style>
                        table {{
                            width: 100%;
                            border-collapse: collapse;
                        }}
                        th, td {{
                            border: 1px solid black;
                            padding: 8px;
                            text-align: left;
                        }}
                        th {{
                            background-color: #f2f2f2;
                        }}
                        img.logo {{
                            position: absolute;
                            top: 0;
                            right: 0;
                            border-radius:50%;
                            height:3.3rem;
                            width:3.3rem;
                        }}

                        h1 {{
                            color: #40E0D0; /* Color celeste */
                        }}
                    </style>
                </head>
                <body>
                    <img src='https://firebasestorage.googleapis.com/v0/b/proyectos-cb445.appspot.com/o/img_logo_inkamanu.jpeg?alt=media&token=3b834c39-f2ee-4555-8770-4f5a2bc88066&_gl=1*gxgr9z*_ga*MTcyOTkyMjIwMS4xNjk2NDU2NzU2*_ga_CW55HF8NVT*MTY5NjQ1Njc1NS4xLjEuMTY5NjQ1NzkyMy40OC4wLjA.' alt='Logo' width='100' class='logo'/>
                    <h1>Reporte de Pedido {id}</h1>
                    <table>
                        <tr>
                            <th>ID</th>
                            <th>UserID</th>
                            <th>Total (en soles)</th>
                      
                            <th>Status</th>
                        </tr>";




                html += $@"
                <tr>
                    <td>{pedido.ID}</td>
                    <td>{pedido.UserID}</td>
                    <td>{pedido.Total}</td>
               
                    <td>{pedido.Status}</td>
           
                </tr>";


                html += @"
                    </table>
                </body>
            </html>";

                var globalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                };
                var objectSettings = new ObjectSettings
                {
                    HtmlContent = html
                };
                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = globalSettings,
                    Objects = { objectSettings }
                };
                var file = _converter.Convert(pdf);
                return File(file, "application/pdf", $"Pedido_{id}.pdf");

            }
            catch (Exception ex)
            {
                // Loguear el error para obtener más detalles
                _logger.LogError(ex, $"Error al exportar el pedido {id} a PDF");
                // Retornar un mensaje de error al usuario
                return StatusCode(500, $"Ocurrió un error al exportar el pedido {id} a PDF. Por favor, inténtelo de nuevo más tarde.");
            }
        }



        public async Task<ActionResult> ExportarUnSoloPedidoEnExcel(int? id)
        {
            try
            {

                if (id == null)
                {
                    return NotFound($"El pedido con ID {id} no fue encontrado, por eso no se puede exportar en Excel.");
                }

                Pedido? pedido = await _context.DataPedido.FindAsync(id);

                if (pedido == null)
                {
                    return NotFound($"El pedido con ID {id} no fue encontrado, por eso no se puede exportar en Excel.");
                }



                var resultados = (from p in _context.DataPedido
                                  where p.ID == id  // Filtrar por ID del pedido, esto es lo unico diferente al metdo de exportar todos en excel
                                  join d in _context.DataDetallePedido on p.ID equals d.pedido.ID
                                  join pa in _context.DataPago on p.pago.Id equals pa.Id
                                  select new
                                  {
                                      IDPedido = p.ID,
                                      UserID = p.UserID,
                                      Total = p.Total,
                                      Status = p.Status,
                                      FechaDePago = pa.PaymentDate,
                                      NombreTarjeta = pa.NombreTarjeta,
                                      //Ultimos4DigitosTarjeta = pa.NumeroTarjeta.Length > 4 ? pa.NumeroTarjeta.Substring(pa.NumeroTarjeta.Length - 4) : pa.NumeroTarjeta,
                                      DigitosTarjeta = pa.NumeroTarjeta,
                                      MontoPagado = pa.MontoTotal,
                                      IDProducto = d.Producto.id,
                                      Cantidad = d.Cantidad,
                                      PrecioUnitario = d.Precio
                                  }).ToList();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Pedido");

                // Agregando un título arriba de la tabla
                worksheet.Cells[1, 1].Value = $"Reporte del Pedido {id}";
                worksheet.Cells[1, 1].Style.Font.Size = 20;
                worksheet.Cells[1, 1].Style.Font.Bold = true;

                // Cargar los datos en la fila 3 para dejar espacio para el título de Reporte de Pedidos
                worksheet.Cells[3, 1].LoadFromCollection(resultados, true);

                // Dar formato a la tabla Reporte de Pedidos
                var dataRange = worksheet.Cells[2, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column];
                var table = worksheet.Tables.Add(dataRange, "Pedido");
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Light6;

                // Estilo para los encabezados de las columnas 
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Font.Bold = true;
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);

                // Ajustar el ancho de las columnas automáticamente
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Pedido_{id}.xlsx");
            }
            catch (Exception ex)
            {
                // Loguear el error para obtener más detalles
                _logger.LogError(ex, $"Error al exportar el pedido {id} a Excel");
                // Retornar un mensaje de error al usuario
                return StatusCode(500, $"Ocurrió un error al exportar el pedido {id} a Excel. Por favor, inténtelo de nuevo más tarde.");
            }
        }

        /* Hasta aqui son los metodos para exportar */




        /* metodo para buscar PEDIDO */

        public async Task<IActionResult> BuscarPedido(string? searchUsername, string? orderStatus)
        {
            // Declara la variable pedidosPagedList una sola vez aquí
            IPagedList<Pedido> pedidosPagedList;

            try
            {
                var pedidos = from o in _context.DataPedido select o;

                if (!String.IsNullOrEmpty(searchUsername) && !String.IsNullOrEmpty(orderStatus))
                {
                    pedidos = pedidos.Where(s => s.UserID.Contains(searchUsername) && s.Status.Contains(orderStatus));
                }
                else if (!String.IsNullOrEmpty(searchUsername))
                {
                    pedidos = pedidos.Where(s => s.UserID.Contains(searchUsername));
                }
                else if (!String.IsNullOrEmpty(orderStatus))
                {
                    pedidos = pedidos.Where(s => s.Status.Contains(orderStatus));
                }

                var pedidosList = await pedidos.ToListAsync();

                if (!pedidosList.Any())
                {
                    TempData["MessageDeRespuesta"] = "No se encontraron pedidos que coincidan con la búsqueda.";
                    pedidosPagedList = new PagedList<Pedido>(new List<Pedido>(), 1, 1);
                }
                else
                {
                    pedidosPagedList = pedidosList.ToPagedList(1, pedidosList.Count);
                }
            }
            catch (Exception ex)
            {
                TempData["MessageDeRespuesta"] = "Ocurrió un error al buscar pedidos. Por favor, inténtalo de nuevo más tarde.";
                pedidosPagedList = new PagedList<Pedido>(new List<Pedido>(), 1, 1);
            }

            // Retorna la vista con pedidosPagedList, que siempre tendrá un valor asignado.
            return View("ListaDePedidos", pedidosPagedList);
        }

        public async Task<ActionResult> EditarPedido(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Pedido? pedido = await _context.DataPedido.FindAsync(id);

            if (pedido == null)
            {
                return NotFound();
            }

            return View("EditarPedido", pedido);
        }

        [HttpPost]
        public async Task<ActionResult> GuardarPedidoEditado(int id, Pedido pedidoActualizado)
        {
            // Validar si el ID del pedido es válido
            Pedido? pedido = await _context.DataPedido.FindAsync(id);

            if (pedido == null)
            {
                return NotFound();
            }

            // Actualizar solo el estado del pedido
            pedido.Status = pedidoActualizado.Status;

            try
            {
                _context.DataPedido.Update(pedido);
                await _context.SaveChangesAsync();
                TempData["MessageActualizandoPedido"] = "Estado del pedido actualizado exitosamente.";
                return RedirectToAction("EditarPedido", new { id = pedido.ID });
            }
            catch (Exception ex)
            {
                // Aquí puedes manejar cualquier error que pueda surgir al intentar actualizar el pedido en la base de datos.
                TempData["ErrorActualizandoPedido"] = "Ocurrió un error al actualizar el estado del pedido. Por favor, inténtalo de nuevo.";
                return View("EditarPedido", pedido);
            }
        }

        public async Task<IActionResult> VerPedido(int? id)
        {
            try
            {
                var pedido = await _context.DataPedido.FirstOrDefaultAsync(p => p.ID == id);

                if (pedido == null)
                {
                    return View("Error", new { message = "Pedido no encontrado." });
                }

                var detalles = (from detalle in _context.DataDetallePedido
                                join producto in _context.DataProducto on detalle.Producto.id equals producto.id
                                where detalle.pedido.ID == pedido.ID
                                select new DetallePedidoViewModel
                                {
                                    Cantidad = detalle.Cantidad,
                                    PrecioUnitario = detalle.Precio,
                                    NombreProducto = producto.Nombre,
                                    DescripcionProducto = producto.Descripcion,
                                    ImagenProducto = producto.Imagen,
                                    // SE PUEDE AGREGAR MAS CAMPOS DE LA TABLA PRODUCTO SI ASI LO QUIERES, ESTO PARA MI ES NECESARIO
                                }).ToList();

                var viewModel = new PedidoViewModel
                {
                    ID = pedido.ID,
                    Status = pedido.Status,
                    Items = detalles,
                    Total = pedido.Total
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Un error inesperado ocurrió mientras se obtenían los detalles del pedido.");
                return View("Error");
            }
        }



        /* query en base de datos oracle para probar una teoria, esta era la idea original solo mostrar los 4 ultimos numeros de la tarjeta pero luego pense mejor muestro toda xd
        SELECT 
            p.id AS "ID Pedido",
            p."UserID",
            p."Total",
            p."Status",
            pa."PaymentDate" AS "Fecha de Pago",
            pa."NombreTarjeta",
            RIGHT(pa."NumeroTarjeta", 4) AS "Últimos 4 dígitos Tarjeta",
            pa."MontoTotal" AS "Monto Pagado",
            d."Productoid" AS "ID Producto",
            d."Cantidad",
            d."Precio" AS "Precio Unitario"
        FROM 
            "t_order" p
        INNER JOIN 
            "t_order_detail" d ON p.id = d."pedidoID"
        INNER JOIN 
            "t_pago" pa ON p."pagoId" = pa.id;
        */













    }
}
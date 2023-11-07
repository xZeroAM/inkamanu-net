using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proyecto_inkamanu_net.Data;
using proyecto_inkamanu_net.Models;
using proyecto_inkamanu_net.Models.Entity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace proyecto_inkamanu_net.Controllers
{
    public class CarritoController : Controller
    {
        private readonly ILogger<CarritoController> _logger;

        private ApplicationDbContext _context;

        /* para el cliente o administrador iniciado */

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly SignInManager<ApplicationUser> _signInManager;

        private readonly ICarritoService _carritoService;
        public CarritoController(ILogger<CarritoController> logger, ApplicationDbContext context,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager, ICarritoService carritoService)
        {
            _logger = logger;


            _context = context;

            /* variables para el objeto iniciado */
            _userManager = userManager;
            _signInManager = signInManager;

            _carritoService = carritoService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = _userManager.GetUserName(User);
                var items = await _carritoService.ObtenerItems(userId);
                var subtotal = await _carritoService.ObtenerSubtotal(userId);
                var descuento = await _carritoService.ObtenerDescuento(userId);
                var igv = subtotal - (subtotal/1.18);
                var total = await _carritoService.ObtenerTotal(userId);
                var cantidadBotellas = await _carritoService.ObtenerCantidadTotalBotellas(userId);

                /*Random random = new Random();
                int numeroAleatorio = random.Next(1, 11); // el 11 es el 10 como maximo

                string? regalo = null;

                if (cantidadBotellas >= 12 && cantidadBotellas <= 35)
                {
                    if (numeroAleatorio >= 1 && numeroAleatorio <= 5)
                    {
                        regalo = "destapador";
                    }
                    else if (numeroAleatorio >= 6 && numeroAleatorio <= 10)
                    {
                        regalo = "vaso";
                    }
                }*/

                int primerDigito = int.Parse(total.ToString()[0].ToString());

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
                
                var viewModel = new CarritoViewModel
                {
                    Items = items.ToList(),
                    Subtotal = subtotal,
                    Descuento = descuento,
                    Igv = igv,
                    Regalo = regalo,
                    Total = total
                };

                return View(viewModel);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Un error ocurrió mientras se interactuaba con la base de datos.");
                return View("DatabaseError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Un error inesperado ocurrió mientras se obtenía el índice del carrito.");
                return View("Error");
            }
        }

        public async Task<IActionResult> QuitarDelCarrito(int id)
        {
            try
            {
                var userId = _userManager.GetUserName(User);
                if (userId == null)
                {
                    throw new Exception("User is not authenticated.");
                }
                var result = await _carritoService.QuitarDelCarrito(id, userId);
                if (result)
                {
                    return RedirectToAction("Index", "Carrito");
                }
                return View("Error");
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Un error ocurrió mientras se interactuaba con la base de datos.");
                return View("DatabaseError");
            }
            catch (EndOfStreamException ex)
            {
                _logger.LogError(ex, "Intento de leer más allá del final del flujo.");
                return View("StreamError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Un error ocurrió mientras se intentaba quitar el ítem con ID {id} del carrito.");
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarCantidad(int id, int cantidad)
        {
            try
            {
                var userId = _userManager.GetUserName(User);
                var result = await _carritoService.ActualizarCantidad(id, cantidad, userId);
                if (result)
                {
                    return RedirectToAction("Index", "Carrito");
                }
                return View("Error");

            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Un error ocurrió mientras se interactuaba con la base de datos.");
                return View("DatabaseError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Un error ocurrió mientras se actualizaba la cantidad del ítem con ID {id}.");
                return View("Error");
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
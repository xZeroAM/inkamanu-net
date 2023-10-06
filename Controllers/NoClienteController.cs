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
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace proyecto_ecommerce_deportivo_net.Controllers
{
    public class NoClienteController : Controller
    {
        private readonly ILogger<NoClienteController> _logger;
        private ApplicationDbContext _context;

        /* para el cliente o administrador iniciado */

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly SignInManager<ApplicationUser> _signInManager;
        public NoClienteController(ILogger<NoClienteController> logger, ApplicationDbContext context,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager)
        {
            _logger = logger;
            _context = context;

            /* variables para el objeto iniciado */
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Catalogo()
        {
            var productos = from o in _context.DataProducto select o;
            return View(productos.ToList());
        }

        [HttpGet] /* este index lo hice de prueba para arreglar el problema del carrusel NO TOCARLO */
        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> BuscarProducto(string query)
        {
            try
            {
                // Si no hay una consulta de búsqueda, retorna todos los productos.
                if (string.IsNullOrWhiteSpace(query))
                {
                    var todosLosProductos = await _context.DataProducto.ToListAsync();
                    return View("Catalogo", todosLosProductos);
                }

                // Convierte la consulta de búsqueda a mayúsculas para la comparación.
                query = query.ToUpper();

                // Busca productos que coincidan con la consulta de búsqueda.
                var productos = await _context.DataProducto
                    .Where(p => p.Nombre.ToUpper().Contains(query))
                    .ToListAsync();

                // Si no se encontraron productos, establece un mensaje en TempData.
                if (!productos.Any())
                {
                    TempData["MessageDeRespuesta"] = "No se encontraron productos que coincidan con la búsqueda.";
                }
                else
                {
                    TempData["MessageDeRespuesta1"] = "Si se encontraron productos que coincidan con la búsqueda.";
                }
                // Retorna la vista Catalogo con la lista de productos.
                return View("Catalogo", productos);
            }
            catch (Exception ex)
            {
                // En caso de error, establece un mensaje en TempData y retorna una lista vacía a la vista.
                TempData["MessageDeRespuesta"] = "Ocurrió un error al buscar productos. Por favor, inténtalo de nuevo más tarde.";
                return View("Catalogo", new List<Producto>());
            }
        }

        public async Task<IActionResult> DetalleProducto(int? id)
        {
            Producto objProduct = await _context.DataProducto.FindAsync(id);
            if (objProduct == null)
            {
                return NotFound();
            }
            return View(objProduct);
        }

        [HttpPost] // Asegúrate de que es un método POST
        public async Task<IActionResult> AddCarrito(int id, int cantidad)
        {
            var userID = _userManager.GetUserName(User); //sesion

            if (userID == null)
            {
                // no se ha logueado
                TempData["MessageLOGUEARSE"] = "Por favor debe loguearse antes de agregar un producto";
                return View("~/Views/Home/Index.cshtml");
            }
            else
            {
                // ya está logueado
                var producto = await _context.DataProducto.FindAsync(id);

                // Buscar una proforma existente para el usuario y producto
                var proformaExistente = await _context.DataCarrito
                    .Where(p => p.UserID == userID && p.Producto.id == id)
                    .FirstOrDefaultAsync();

                if (proformaExistente != null)
                {
                    // Si existe, actualizar la cantidad
                    proformaExistente.Cantidad = cantidad;
                }
                else
                {
                    // Si no existe, crear una nueva proforma
                    Proforma proforma = new Proforma
                    {
                        Producto = producto,
                        Precio = producto.Precio,
                        Cantidad = cantidad, // Usa la cantidad pasada desde el formulario
                        UserID = userID
                    };
                    _context.Add(proforma);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Carrito");
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
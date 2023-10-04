using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using proyecto_inkamanu_net.Models;
using Microsoft.Extensions.Logging;
using proyecto_inkamanu_net.Data;
using proyecto_inkamanu_net.Models.Entity;

namespace proyecto_ecommerce_deportivo_net.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    private readonly IMyEmailSender _emailSender;
    public HomeController(ILogger<HomeController> logger,
        ApplicationDbContext context, IMyEmailSender emailSender)
    {
        _logger = logger;

        /* lineas agregadas */
        _context = context;

        _emailSender = emailSender;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Contacto()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Contacto objContacto)
    {
        _context.Add(objContacto);
        await _context.SaveChangesAsync();

        var message = $"Estimado {objContacto.Nombre}, te estaremos contactando pronto";
        TempData["MessageCONTACTO"] = message;

        await _emailSender.SendEmailAsync(objContacto.Email, "Gracias por contactarnos", message);

        return View("~/Views/Home/Contacto.cshtml");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

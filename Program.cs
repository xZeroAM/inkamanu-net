using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using proyecto_inkamanu_net.Data;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Diagnostics;
using proyecto_inkamanu_net.Models;
using System.Configuration;

/* PARA EXPORTAR A PDF Y EXCEL */
using DinkToPdf;
using DinkToPdf.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

// Add services to the container.
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var connectionString = Environment.GetEnvironmentVariable("RENDER_POSTGRES_CONNECTION");
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("PostgresSQLConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    //options.UseSqlite(connectionString));
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Aquí es donde debes agregar la configuración del servicio de carrito.
builder.Services.AddTransient<ICarritoService, CarritoService>();

// Aquí es donde debes hacer el cambio, usa builder.Configuration en lugar de Configuration
builder.Services.AddTransient<IMyEmailSender, EmailSender>(i =>
        new EmailSender(
            builder.Configuration["Email:SmtpServer"],
            int.Parse(builder.Configuration["Email:SmtpPort"]),
            builder.Configuration["Email:SmtpUsername"],
            builder.Configuration["Email:SmtpPassword"]
        )
    );

/* CONFIGURANDO PARA EXPORTAR EN EXCEL Y PDF */
// Registro del convertidor de DinkToPdf
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));


builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "custom",
        pattern: "{company}/{controller}/{action}/{id?}",
        defaults: new { company = "InkaManu", controller = "NoCliente", action = "Catalogo" });
app.MapRazorPages();

app.MapHealthChecks("/health");

app.Run();



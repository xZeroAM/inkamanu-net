using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using proyecto_inkamanu_net.Models;
using proyecto_inkamanu_net.Models.Entity;

namespace proyecto_inkamanu_net.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Producto> DataProducto  {get; set;}
    public DbSet<Proforma> DataCarrito { get; set; }

    public DbSet<Contacto> DataContactos { get; set; }
}

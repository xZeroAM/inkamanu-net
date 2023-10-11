using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_inkamanu_net.Models.Entity;

namespace proyecto_inkamanu_net.Models
{
    public interface ICarritoService
    {
        Task<IEnumerable<Proforma>> ObtenerItems(string userId);
        Task<bool> ActualizarCantidad(int id, int cantidad, string userId);
        Task<double> ObtenerSubtotal(string userId);
        Task<double> ObtenerDescuento(string userId);
        Task<double> ObtenerTotal(string userId);

        Task<bool> QuitarDelCarrito(int id, string userId);

        Task<int> ObtenerCantidadTotalBotellas(string userId);
    }
}
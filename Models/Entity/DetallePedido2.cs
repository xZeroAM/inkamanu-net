using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proyecto_inkamanu_net.Models.Entity
{
    public class DetallePedido2
    {
        public int Cantidad { get; set; }
        public double PrecioUnitario { get; set; }
        public string NombreProducto { get; set; }
        public string DescripcionProducto { get; set; }
        public string ImagenProducto { get; set; }

        public double Importe { get; set; }
    }
}
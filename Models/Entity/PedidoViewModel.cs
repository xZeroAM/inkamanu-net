using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_inkamanu_net.Models.Entity;

namespace proyecto_inkamanu_net.Models.Entity
{
    public class PedidoViewModel
    {
        public int ID { get; set; }
        public string? Status { get; set; }
        public List<DetallePedidoViewModel> Items { get; set; }
        public double Total { get; set; }
    }
}
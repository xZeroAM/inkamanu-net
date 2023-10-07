using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proyecto_inkamanu_net.Models.Entity
{
     [Table("t_order_detail")]
    public class DetallePedido
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int ID {get; set;}

        public Producto? Producto {get; set;}

        public int Cantidad{get; set;}
        public Double Precio { get; set; }
        public Pedido? pedido {get; set;}
    }
}
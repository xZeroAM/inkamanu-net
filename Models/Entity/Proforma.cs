using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace proyecto_inkamanu_net.Models.Entity {
    [Table("t_proforma")]
    public class Proforma {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]        
        public int Id { get; set; }   
        public string? UserID { get; set; }
        public Producto? Producto { get; set; }
        public int Cantidad { get; set; }

        public Double Precio { get; set; }
        public String Status { get; set; } ="PENDIENTE";
    }
}
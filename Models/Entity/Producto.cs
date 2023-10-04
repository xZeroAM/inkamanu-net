using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace proyecto_inkamanu_net.Models.Entity
{
    public class Producto
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        [NotNull]
        public string Nombre { get; set; }

        [NotNull]
        public string Descripcion { get; set; }

        [NotNull]
        public string Imagen { get; set; }

        [NotNull]
        public Double Precio { get; set; }

        [NotNull]
        public int Stock { get; set; }

        [NotNull]
        public DateTime fechaCreacion { get; set; }

        public DateTime? fechaActualizacion { get; set; }
    }
}
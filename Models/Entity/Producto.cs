using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace proyecto_inkamanu_net.Models.Entity
{
    [Table("t_producto")]
    public class Producto
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int id { get; set; }

        [NotNull]
        public string? Nombre { get; set; }

        [NotNull]
        public string? Descripcion { get; set; }

        [NotNull]
        public string? Imagen { get; set; }

        [NotNull]
        public Double Precio { get; set; }

        [NotNull]
        public int Stock { get; set; }

        [NotNull]
        public Double? GraduacionAlcoholica { get; set; }

        [NotNull]
        public string? TipoCerveza { get; set; }

        [NotNull]
        public Double? Volumen { get; set; }

        [NotNull]
        public string? TipoEnvase { get; set; }


        [NotNull]
        public DateTime? fechaCreacion { get; set; }

        public DateTime? fechaActualizacion { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace proyecto_inkamanu_net.Models.Entity {

     [Table("t_contacto")]
    public class Contacto {
        //EL SIGNO DE INTERROGACION ES PARA DEJAR VALORES NULOS
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("Nombre")]
        public string? Nombre { get; set; }

        [Column("Email")]
        public string? Email { get; set; }

        [Column("Phone")]
        public string? Phone { get; set; }

        [Column("Asunto")]
        public string? Asunto { get; set; }

        [Column("Mensaje")]
        public string? Mensaje { get; set; }
    }
}
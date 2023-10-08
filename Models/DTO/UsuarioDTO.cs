using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace proyecto_inkamanu_net.Models.DTO {

    public class UsuarioDTO {
        public string? Id;
        public string? Nombres;
        public string? ApellidoPaterno;
        public string? ApellidoMaterno;
        public string? Email;
        public string? Dni;
        public string? Celular;
        public string? Genero;
    }

}


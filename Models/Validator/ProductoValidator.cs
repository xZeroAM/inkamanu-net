using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using proyecto_inkamanu_net.Models.DTO;

namespace proyecto_inkamanu_net.Models.Validator
{
    public class ProductoValidator : AbstractValidator<ProductoDTO>
    {
        public ProductoValidator()
        {
            RuleFor(producto => producto.Nombre)
           .NotEmpty().WithMessage("El campo de Nombre es obligatorio")
           .NotNull().WithMessage("El campo de nombre es obligatorio");

            RuleFor(producto => producto.Descripcion)
                .NotEmpty().WithMessage("El campo Descripcion es obligatorio")
                .NotNull().WithMessage("El campo de descripcion es obligatorio");

            RuleFor(producto => producto.Precio)
                .GreaterThan(0).WithMessage("El valor del precio debe ser mayor que 0")
                .NotEmpty().WithMessage("El campo no puede estar vacio");

            RuleFor(producto => producto.Stock)
                .GreaterThanOrEqualTo(0).WithMessage("El valor del stock debe ser mayor o igual a 0");
        }


    }
}
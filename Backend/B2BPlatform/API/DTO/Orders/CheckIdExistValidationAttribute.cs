using Entities;
using Interfaces;
using System.ComponentModel.DataAnnotations;

namespace API.DTO.Orders
{
    internal class CheckIdExistValidationAttribute<T> : ValidationAttribute
        where T : class
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
                return null;
            IUnitOfWork? unitOfWork = validationContext.GetService<IUnitOfWork>();
            bool check = false;
            if (unitOfWork is null)
                return new ValidationResult("the Service can't provide");
            else if (typeof(T) == typeof(Order))
                check = unitOfWork.Orders.GetById((int)value) is not null;
            else if (typeof(T) == typeof(Supplier))
                check = unitOfWork.Suppliers.GetById((int)value) is not null;
            else if (typeof(T) == typeof(User))
                check = unitOfWork.Users.GetById((int)value) is not null;
            else if (typeof(T) == typeof(Deal))
                check = unitOfWork.Deals.GetById((int)value) is not null;
            else if (typeof(T) == typeof(Category))
                check = unitOfWork.Categories.GetById((int)value) is not null;
            if (check == true)
                return ValidationResult.Success;
            return new ValidationResult($"the id of {typeof(T).Name} is invalid");
        }
    }


}

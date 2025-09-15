using Entities;
using Interfaces;
using System.ComponentModel.DataAnnotations;

namespace API.DTO
{
    internal class IsUniqueValidationAttribute<T> : ValidationAttribute
    where T : class
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
                return null;
            IUnitOfWork? unitOfWork = validationContext.GetService<IUnitOfWork>();
            if (unitOfWork is null)
                return new ValidationResult("the Service can't provide");
            if(typeof(T) == typeof(User))
            {
                string propertyName = validationContext.DisplayName ?? validationContext.MemberName;
                if ( propertyName == "email" && unitOfWork.Users.CheckExistByEmail(value.ToString()))
                    return new ValidationResult("إيميل مستخدم من قبل");
                else if(propertyName == "UserName" && unitOfWork.Users.CheckExistByUserName(value.ToString()))
                    return new ValidationResult("اسم مستخدم من قبل");
            }
            //else if (typeof(T) == typeof(Supplier) && unitOfWork.Suppliers.CheckExistByCompanyName(value.ToString()))
            //    return new ValidationResult("The company name  must be unique");
            //else if (typeof(T) == typeof(Supplier) && unitOfWork.Suppliers.CheckExistByCompanyName(value.ToString()))
            //    return new ValidationResult("The company name  must be unique");
            //else if (typeof(T) == typeof(Dependent) && unitOfWork.Dependents.GetByNameAsync(value.ToString()).Result is not null)
            //    return new ValidationResult("the name of dependent must be unique");
            //else if (typeof(T) == typeof(Admin) && unitOfWork.Admins.GetAll().FirstOrDefault(x => value.ToString() == x.SSN) is not null)
            //    return new ValidationResult("the SSN of Admin must be unique");
            return ValidationResult.Success;
        }
    }
}

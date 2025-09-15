using System;
using System.ComponentModel.DataAnnotations;
namespace API.ValidationAttributes;
public class FutureDateAttribute : ValidationAttribute
{
    public FutureDateAttribute()
    {
        ErrorMessage = "التاريخ يجب أن يكون أكبر من اليوم";
    }

    public override bool IsValid(object? value)
    {
        if (value is DateTime date)
        {
            return date > DateTime.UtcNow;
        }
        return false;
    }
}

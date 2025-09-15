using API.DTO.Orders;
using Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO.Categories
{
    public class UpdateCategoryDto:AddCategoryDto
    {
        [Required(ErrorMessage = "مطلوب category Id ")]
        [JsonPropertyName("categoryId")]
        [CheckIdExistValidation<Category>]
        public int CategoryId { get; set; }

    }



}

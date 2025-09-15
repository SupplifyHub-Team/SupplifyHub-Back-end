//using API.ValidationAttributes;
//using Entities;
//using System.ComponentModel.DataAnnotations;
//using System.Text.Json.Serialization;

//namespace API.DTO.Orders
//{
//    public class AddDetailsOfOrderAndReviewForClient()
//    {
//        [Required(ErrorMessage = "حقل مطلوب OrderId ")]
//        [JsonPropertyName("orderId")]
//        [CheckIdExistValidation<Order>]
//        public int OrderId { get; set; }

//        [Required(ErrorMessage = "حقل مطلوب userId")]
//        [Range(1, int.MaxValue, ErrorMessage = "قيمة غير صالحة لرقم المورّد. يجب أن يكون عدداً صحيحاً موجباً")]
//        [JsonPropertyName("userId")]
//        [CheckIdExistValidation<User>]
//        public int UserId { get; set; }


//        [Required(ErrorMessage = "تاريخ اتمام الاتفاق حقل مطلوب ")]
//        [JsonPropertyName("dealDoneAt")]
//        public DateTime DealDoneAt { get; set; }

//        //[Required(ErrorMessage = "الكمية حقل مطلوب")]
//        //[Range(1, int.MaxValue, ErrorMessage = "قيمة غير صالحة للكمية. يجب أن تكون عدداً صحيحاً موجباً")]
//        //[JsonPropertyName("quantity")]
//        //public int Quantity { get; set; }
//        [Required(ErrorMessage = "الوصف حقل مطلوب")]
//        [MaxLength(2000, ErrorMessage = " الكمية والوصف لا يجب أن يتجاوز 2000 حرف")]
//        [JsonPropertyName("description")]
//        public string DescriptionAndQuantity { get; set; }

//        [Required(ErrorMessage = "السعر حقل مطلوب")]
//        [Range(1, double.MaxValue, ErrorMessage = "قيمة غير صالحة للسعر. يجب أن تكون عدداً صحيحاً موجباً")]
//        [JsonPropertyName("price")]
//        public double Price { get; set; }

//        [Required(ErrorMessage = "وقت الوصول حقل مطلوب")]
//        [JsonPropertyName("dateOfDelivered")]
//        public DateTime DateOfDelivered { get; set; }

//        [Required(ErrorMessage = "التقييم حقل مطلوب")]
//        [Range(1, 5, ErrorMessage = "قيمة غير صالحة للتقييم. يجب أن تكون بين 1 و 5")]
//        [JsonPropertyName("rating")]
//        public int Rating { get; set; }

//        [Required(ErrorMessage = "التعليق حقل مطلوب")]
//        [MaxLength(2000, ErrorMessage = "التعليق لا يجب أن يتجاوز 2000 حرف")]
//        [JsonPropertyName("comment")]
//        public string Comment { get; set; }
//    }


//}
using API.ValidationAttributes;
using Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO.Orders
{
    public class AddDetailsOfOrderAndReviewForClient
    {
        [Required(ErrorMessage = "حقل مطلوب OrderId ")]
        [JsonPropertyName("orderId")]
        [CheckIdExistValidation<Order>]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "حقل مطلوب userId")]
        [Range(1, int.MaxValue, ErrorMessage = "قيمة غير صالحة لرقم المورّد. يجب أن يكون عدداً صحيحاً موجباً")]
        [JsonPropertyName("userId")]
        [CheckIdExistValidation<User>]
        public int UserId { get; set; }

        [Required(ErrorMessage = "تاريخ اتمام الاتفاق حقل مطلوب ")]
        [JsonPropertyName("dealDoneAt")]
        public DateTime DealDoneAt { get; set; }

        //[Required(ErrorMessage = "الوصف حقل مطلوب")]
        //[MaxLength(2000, ErrorMessage = " الكمية والوصف لا يجب أن يتجاوز 2000 حرف")]
        //[JsonPropertyName("description")]
        //public string DescriptionAndQuantity { get; set; }

        [Required(ErrorMessage = "وقت الوصول حقل مطلوب")]
        [JsonPropertyName("dateOfDelivered")]
        public DateTime DateOfDelivered { get; set; }

        [Required(ErrorMessage = "التقييم حقل مطلوب")]
        [Range(1, 5, ErrorMessage = "قيمة غير صالحة للتقييم. يجب أن تكون بين 1 و 5")]
        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "التعليق حقل مطلوب")]
        [MaxLength(2000, ErrorMessage = "التعليق لا يجب أن يتجاوز 2000 حرف")]
        [JsonPropertyName("comment")]
        public string Comment { get; set; }

        [Required(ErrorMessage = "يجب ادخال عناصر الطلب")]
        [JsonPropertyName("items")]
        public List<AddDealItemDto> Items { get; set; } = new List<AddDealItemDto>();
    }
}

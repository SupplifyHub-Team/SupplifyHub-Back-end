using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Models.Entities;
using System.ComponentModel.DataAnnotations;
namespace Entities;

public class DealDetailsVerification
{
    [Key]
    public int Id { get; set; }
    public int DealId { get; set; }
    public int SubmittedById { get; set; }
    //public string DiscriptionAndQuantity { get; set; }
    //public int Quantity { get; set; }
    //public double Price { get; set; }
    public DateTime DealDoneAt { get; set; }
    public DateTime DateOfDelivered { get; set; }
    public DateTime SubmittedAt { get; set; }
    public Deal Deal { get; set; }
    public User User { get; set; }
    public List<DealItem> Items { get; set; }
}

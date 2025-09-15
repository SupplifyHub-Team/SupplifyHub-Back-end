using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
namespace Entities;

public class Review
{
    [Key]
    public int Id { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int DealId { get; set; }
    public int ReviewerId { get; set; }
    public int RevieweeId { get; set; }
    public Deal Deal { get; set; }
    public User Reviewer { get; set; }
    public User Reviewee { get; set; }
}
using Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Entities
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        // Foreign Key
        public int OrderId { get; set; }

        
        public string Name { get; set; }

        [Required]
        public int Quantity { get; set; }

        public string? Notes { get; set; }
        public Order Order { get; set; }

    }
}

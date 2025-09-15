using Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Entities
{
    public class SupplierAdvertisement
    {
        [Key]
        public int Id { get; set; }

        
        public int? SupplierId { get; set; } // FK to Supplier

        public string Title { get; set; } 

        public string ImageUrl { get; set; } // store path/URL to image
        public string ImagePublicId { get; set; }

        public string TargetUrl { get; set; } // optional description
        public DateTime StartDate { get; set; } // optional scheduling
        public DateTime EndDate { get; set; }   // optional scheduling

        public bool IsActive { get; set; } = true; // control visibility

        public DateTime CreatedAt { get; set; } 
        public DateTime? UpdatedAt { get; set; }
        public int? impressions { get; set; }
        public int? clicks { get; set; }

        // Navigation Property
        public Supplier? Supplier { get; set; }
    }
}

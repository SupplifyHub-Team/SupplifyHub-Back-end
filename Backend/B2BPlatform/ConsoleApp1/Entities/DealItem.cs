using Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Entities
{
    public class DealItem
    {
        public int Id { get; set; }
        public int DealDetailsVerificationId { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public DealDetailsVerification DealDetailsVerification { get; set; }
    }
}

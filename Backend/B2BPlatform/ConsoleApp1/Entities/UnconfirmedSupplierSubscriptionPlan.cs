using Entities;
using Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class UnconfirmedSupplierSubscriptionPlan
    {
        public Guid Id { get; set; }
        public int SupplierId { get; set; }
        public int PlanId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Supplier Supplier { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; }
    }

}

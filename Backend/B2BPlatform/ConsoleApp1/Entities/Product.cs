using Entities;
using Enum;
namespace Models.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProductImageURl { get; set; }
        public string ImagePublicId { get; set; }
        public double Price { get; set; }
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }
        public SpecialProduct SpecialProduct { get; set; }
    }
}

namespace Models.Entities
{
    public class SpecialProduct
    {
        public int ProductId { get; set; }
        public int Offer { get; set; }
        public Product Product { get; set; }

    }
}

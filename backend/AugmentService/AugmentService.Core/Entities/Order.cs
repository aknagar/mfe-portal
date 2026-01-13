namespace AugmentService.Core.Entities
{
    public class Order
    {
        public required string Name { get; set; }
        public int TotalCost { get; set; }
        public int Quantity { get; set; }
    }
}


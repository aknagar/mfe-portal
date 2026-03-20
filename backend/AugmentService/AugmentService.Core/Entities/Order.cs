using System.ComponentModel.DataAnnotations;

namespace AugmentService.Core.Entities
{
    public class Order
    {
        [Required]
        [MinLength(1, ErrorMessage = "Name must not be empty.")]
        public required string Name { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "TotalCost must be a non-negative value.")]
        public int TotalCost { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }
}


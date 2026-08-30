using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class TestTypeConsumable
    {
        [Key]
        public int TestTypeConsumableId { get; set; }

        public int TestTypeId { get; set; }

        public virtual TestType? TestType { get; set; }

        public int ConsumableId { get; set; }

        public virtual Consumable? Consumable { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public int QuantityRequired { get; set; }
    }
}
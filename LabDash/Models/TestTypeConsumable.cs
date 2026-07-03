using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class TestTypeConsumable
    {
        [Key]
        public int TestTypeConsumableId { get; set; }

        public int TestTypeId { get; set; }

        public TestType TestType { get; set; }

        public int ConsumableId { get; set; }

        public Consumable Consumable { get; set; }

        // Quantity used for ONE test
        public int QuantityRequired { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabDash.Models
{
    public class ConsumableOrderItem
    {
        [Key]
        public int ConsumableOrderItemId { get; set; }

        [Required]
        public int ConsumableOrderId { get; set; }

        public virtual ConsumableOrder ConsumableOrder { get; set; }

        [Required]
        public int ConsumableId { get; set; }

        public virtual Consumable Consumable { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int QuantityOrdered { get; set; }

        public DateTime? DateReceived { get; set; }

        public DateTime? DateCancelled { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Ordered";
    }
}
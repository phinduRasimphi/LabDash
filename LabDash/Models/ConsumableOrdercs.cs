using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class ConsumableOrder
    {
        [Key]
        public int ConsumableOrderId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Order Number")]
        public string OrderNumber { get; set; }

        [Required]
        public int SupplierId { get; set; }

        public virtual Supplier Supplier { get; set; }

        [Required]
        public DateTime DateOrdered { get; set; }

        public DateTime? DateCompleted { get; set; }

        public DateTime? DateCancelled { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Ordered";

        public virtual ICollection<ConsumableOrderItem> Items { get; set; }
            = new List<ConsumableOrderItem>();
    }
}
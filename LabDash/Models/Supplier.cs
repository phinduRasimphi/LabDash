using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class Supplier
    {
        [Key]
        public int SupplierId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }

        public virtual ICollection<ConsumableOrder> ConsumableOrders { get; set; }
            = new List<ConsumableOrder>();
    }
}
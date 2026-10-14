using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabDash.Models
{
    public class TestReviewHistory
    {
        [Key]
        public int TestReviewHistoryId { get; set; }

        [Required]
        public int TestRequestItemId { get; set; }

        [ForeignKey(nameof(TestRequestItemId))]
        public virtual TestRequestItem? TestRequestItem { get; set; }

        [Required]
        public DateTime ReturnedAt { get; set; } = DateTime.Now;

        [Required]
        public string ReviewReason { get; set; } = string.Empty;

        public string? ReturnedBy { get; set; }
    }
}

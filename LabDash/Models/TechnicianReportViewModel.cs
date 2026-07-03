using System.ComponentModel.DataAnnotations;

namespace LabDash.Models.ViewModels
{
    public class TechnicianReportViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }
    }
}
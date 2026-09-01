using DocuSign.eSign.Model;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LabDash.ViewModels
{
    public class TestRequestViewModel
    {

        [Required]
        public int RequestID { get; set; }
     

       
        public string PatientName { get; set; } = string.Empty;
        public string PatientSurname { get; set; } = string.Empty;
        public string PatientIDNumber { get; set; } = string.Empty;
        public DateTime PatientDOB { get; set; }
        public string PatientCellphone { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string? MedicalConditions { get; set; }
        public string? Allergies { get; set; }
        public string? Medication { get; set; }
        public string Status { get; set; } = "Submitted";

        [Required, Display(Name = "Request Date")]
        [DataType(DataType.Date)]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required, Display(Name = "Urgency")]
        public string Urgency { get; set; } = "Routine";

        [Display(Name = "Clinical Notes")]
        public string? ClinicalNotes { get; set; }

        [Required(ErrorMessage = "Select at least one test type.")]
        [Display(Name = "Test Types")]
        public List<int> SelectedTestTypeIds { get; set; } = new();

        public List<SelectListItem> AvailableTestTypes { get; set; } = new();

        [Required(ErrorMessage = "Enter at least one sample barcode.")]
        [Display(Name = "Sample Barcodes")]
        public int PatientId { get; set; }
        

        public string Barcode { get; set; } = string.Empty;
    }
}
    


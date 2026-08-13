// ViewModels/ManagePatientsViewModel.cs
namespace LabDash.ViewModels
{
    public class ManagePatientsViewModel
    {
        public string? SearchIDNumber { get; set; }
        public bool HasSearched { get; set; }
        public PatientDetailsViewModel? SearchResult { get; set; }
    }
}
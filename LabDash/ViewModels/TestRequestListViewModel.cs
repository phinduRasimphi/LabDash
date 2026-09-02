using System;
using System.Collections.Generic;

namespace LabDash.ViewModels
{
    public class TestRequestListViewModel
    {
        public int RequestId { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public DateTime RequestDate { get; set; }
        public string Urgency { get; set; }
        public string Status { get; set; }
        public bool HasAbnormalResults { get; set; }
        public int ResultCount { get; set; }
        public List<string> TestTypeNames { get; set; } = new List<string>();
        public string TestTypesDisplay { get; set; }


        // Barcode properties
        public List<string> SampleBarcodes { get; set; } = new List<string>();
        public string SampleBarcodesString { get; set; }
        public string CancellationReason { get; set; }

        // ===== CLINICAL NOTES =====
        public string ClinicalNotes { get; set; }
    }
}
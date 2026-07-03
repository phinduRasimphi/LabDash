using System.ComponentModel.DataAnnotations;

namespace LabDash.Models
{
    public class Sample
    {
        [Key]
        public int SampleId { get; set; }

        public string Barcode { get; set; }

        // Blood, Urine, Swab, etc.
        public string SampleType { get; set; }

        public bool IsReceived { get; set; }

        public DateTime? DateReceived { get; set; }

        public string ReceivedByTechnician { get; set; }

        public int TestRequestId { get; set; }

        public virtual TestRequest TestRequest { get; set; }
    }

}
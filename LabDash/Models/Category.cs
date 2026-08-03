namespace LabDash.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = null!;

        // "Condition", "Allergy", "Medication" - lets us reuse this table
        // for Allergies/Medications later without creating 3 separate tables
        public string Type { get; set; } = null!;

        public bool IsActive { get; set; } = true;
    }
}
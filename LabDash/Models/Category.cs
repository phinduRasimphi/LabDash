namespace LabDash.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        public string Name { get; set; } = null!;

        // "Condition", "Allergy", "Medication"
        // Lets us reuse this table for different types.
        public string Type { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        // Medical Conditions belonging to this category
        public virtual ICollection<MedicalCondition> MedicalConditions { get; set; }
            = new List<MedicalCondition>();

        // Allergies belonging to this category
        public virtual ICollection<Allergy> Allergies { get; set; }
            = new List<Allergy>();
    }
}
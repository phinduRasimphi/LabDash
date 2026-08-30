namespace LabDash.Models
{
    public class Consumable
    {
        public int ConsumableID { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Category { get; set; }

        public DateTime? Expiry { get; set; }

        public int ReorderLevel { get; set; }

        public int StockLevel { get; set; }
        public string? SupplierName { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public ICollection<TestTypeConsumable> TestTypeConsumables
        { get; set; } = new List<TestTypeConsumable>();
    }
}
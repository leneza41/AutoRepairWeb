namespace AutoRepairCore.Models
{
    public class Replacement
    {
        public int ReplacementID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public string? Supplier { get; set; }

        public ICollection<OrderReplacement> OrderReplacements { get; set; } = new List<OrderReplacement>();
    }
}

namespace AutoRepairCore.Models
{
    public class Service
    {
        public int ServiceID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Cost { get; set; }
        public int EstimatedTime { get; set; }

        public ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();
    }
}

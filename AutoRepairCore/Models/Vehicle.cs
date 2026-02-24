namespace AutoRepairCore.Models
{
    public class Vehicle
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public int Mileage { get; set; }
        public string? Type { get; set; }
        public int? Antiquity { get; set; }
        public int CustomerID { get; set; }

        public Customer Customer { get; set; } = null!;
        public ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();
    }
}

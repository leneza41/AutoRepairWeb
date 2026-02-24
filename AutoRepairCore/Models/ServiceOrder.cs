namespace AutoRepairCore.Models
{
    public class ServiceOrder
    {
        public int Folio { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime EstimatedDeliveryTime { get; set; }
        public DateTime? DeliveryTime { get; set; }
        public string State { get; set; } = "Abierta";
        public decimal Cost { get; set; }
        public string SerialNumber { get; set; } = string.Empty;

        public Vehicle Vehicle { get; set; } = null!;
        public ICollection<OrderReplacement> OrderReplacements { get; set; } = new List<OrderReplacement>();
        public ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();
        public ICollection<OrderMechanic> OrderMechanics { get; set; } = new List<OrderMechanic>();
    }
}

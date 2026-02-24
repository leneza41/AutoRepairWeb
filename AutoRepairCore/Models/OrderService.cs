namespace AutoRepairCore.Models
{
    public class OrderService
    {
        public int ServiceID { get; set; }
        public int Folio { get; set; }
        public int Quantity { get; set; }

        public Service Service { get; set; } = null!;
        public ServiceOrder ServiceOrder { get; set; } = null!;
    }
}

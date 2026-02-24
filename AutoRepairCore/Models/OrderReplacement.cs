namespace AutoRepairCore.Models
{
    public class OrderReplacement
    {
        public int ReplacementID { get; set; }
        public int Folio { get; set; }
        public int Quantity { get; set; }

        public Replacement Replacement { get; set; } = null!;
        public ServiceOrder ServiceOrder { get; set; } = null!;
    }
}

namespace AutoRepairCore.Models
{
    public class OrderMechanic
    {
        public int EmployeeID { get; set; }
        public int Folio { get; set; }

        public Mechanic Mechanic { get; set; } = null!;
        public ServiceOrder ServiceOrder { get; set; } = null!;
    }
}

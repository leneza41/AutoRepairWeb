namespace AutoRepairCore.Models
{
    public class Mechanic
    {
        public int EmployeeID { get; set; }
        public string RFC { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FirstLastname { get; set; } = string.Empty;
        public string? SecondLastname { get; set; }
        public string Fields { get; set; } = "Otros";
        public string Phone { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public int Experience { get; set; }

        public ICollection<OrderMechanic> OrderMechanics { get; set; } = new List<OrderMechanic>();
    }
}

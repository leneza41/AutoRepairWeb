namespace AutoRepairCore.Models
{
    public class Customer
    {
        public int CustomerID { get; set; }
        public string RFC { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FirstLastname { get; set; } = string.Empty;
        public string? SecondLastname { get; set; }
        public string Street { get; set; } = string.Empty;
        public string StreetNumber { get; set; } = string.Empty;
        public string Suburb { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string MainPhone { get; set; } = string.Empty;
        public string? SecondaryPhone1 { get; set; }
        public string? SecondaryPhone2 { get; set; }
        public string? Email { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}

using Microsoft.EntityFrameworkCore;
using AutoRepairCore.Models;

namespace AutoRepairCore.Data
{
    // Contexto principal de la base de datos AutoRepairDB
    public class AutoRepairDbContext : DbContext
    {
        public AutoRepairDbContext(DbContextOptions<AutoRepairDbContext> options) : base(options) { }

        // Tablas principales
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Mechanic> Mechanics { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Replacement> Replacements { get; set; }
        public DbSet<ServiceOrder> ServiceOrders { get; set; }

        // Tablas intermedias (relaciones N:M)
        public DbSet<OrderReplacement> OrderReplacements { get; set; }
        public DbSet<OrderService> OrderServices { get; set; }
        public DbSet<OrderMechanic> OrderMechanics { get; set; }

        // Configura las entidades, relaciones y triggers registrados
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapea Customer a la tabla Customers y define restricciones de columnas
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerID);
                // RFC máximo 13 caracteres (formato mexicano)
                entity.Property(e => e.RFC).IsRequired().HasMaxLength(13);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FirstLastname).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(50);
            });

            // Mapea Vehicle, define SerialNumber como PK y registra los triggers de BD
            modelBuilder.Entity<Vehicle>(entity =>
            {
                // La PK es el número de serie, no un entero autoincremental
                entity.HasKey(e => e.SerialNumber);
                entity.Property(e => e.SerialNumber).HasMaxLength(50);
                entity.Property(e => e.PlateNumber).IsRequired().HasMaxLength(15);
                // Al eliminar un cliente se eliminan sus vehículos en cascada
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Vehicles)
                    .HasForeignKey(e => e.CustomerID)
                    .OnDelete(DeleteBehavior.Cascade);
                // Informa a EF que existen triggers para que no use OUTPUT en sus queries
                entity.ToTable(t =>
                {
                    t.HasTrigger("before_vehicle_insert");
                    t.HasTrigger("before_vehicle_update");
                });
            });

            // Mapea Mechanic y define el tipo exacto de la columna Salary en SQL Server
            modelBuilder.Entity<Mechanic>(entity =>
            {
                entity.HasKey(e => e.EmployeeID);
                // RFC máximo 13 caracteres (formato mexicano)
                entity.Property(e => e.RFC).IsRequired().HasMaxLength(13);
                // decimal(10,2) para evitar pérdida de precisión en el salario
                entity.Property(e => e.Salary).HasColumnType("decimal(10,2)");
            });

            // Mapea Service y define el tipo exacto de la columna Cost en SQL Server
            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasKey(e => e.ServiceID);
                // decimal(10,2) para evitar pérdida de precisión en el costo
                entity.Property(e => e.Cost).HasColumnType("decimal(10,2)");
            });

            // Mapea Replacement y define el tipo exacto de la columna UnitPrice en SQL Server
            modelBuilder.Entity<Replacement>(entity =>
            {
                entity.HasKey(e => e.ReplacementID);
                // decimal(10,2) para evitar pérdida de precisión en el precio unitario
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(10,2)");
            });

            // Mapea ServiceOrder, define mayor precisión en Cost y bloquea borrado en cascada
            modelBuilder.Entity<ServiceOrder>(entity =>
            {
                entity.HasKey(e => e.Folio);
                // decimal(12,2) — rango mayor porque Cost incluye IVA
                entity.Property(e => e.Cost).HasColumnType("decimal(12,2)");
                // Restrict: impide eliminar un vehículo si tiene órdenes asociadas
                entity.HasOne(e => e.Vehicle)
                    .WithMany(v => v.ServiceOrders)
                    .HasForeignKey(e => e.SerialNumber)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Mapea OrderReplacement con PK compuesta (una refacción no se repite en la misma orden)
            modelBuilder.Entity<OrderReplacement>(entity =>
            {
                entity.HasKey(e => new { e.ReplacementID, e.Folio });
                entity.HasOne(e => e.Replacement)
                    .WithMany(r => r.OrderReplacements)
                    .HasForeignKey(e => e.ReplacementID);
                entity.HasOne(e => e.ServiceOrder)
                    .WithMany(s => s.OrderReplacements)
                    .HasForeignKey(e => e.Folio);
            });

            // Mapea OrderService con PK compuesta y registra el trigger que recalcula el costo de la orden
            modelBuilder.Entity<OrderService>(entity =>
            {
                // Un servicio no se repite en la misma orden
                entity.HasKey(e => new { e.ServiceID, e.Folio });
                entity.HasOne(e => e.Service)
                    .WithMany(s => s.OrderServices)
                    .HasForeignKey(e => e.ServiceID);
                entity.HasOne(e => e.ServiceOrder)
                    .WithMany(s => s.OrderServices)
                    .HasForeignKey(e => e.Folio);
                // Informa a EF del trigger para que no use OUTPUT; el trigger recalcula Cost con IVA
                entity.ToTable(t =>
                {
                    t.HasTrigger("trg_UpdateServiceOrderCost");
                });
            });

            // Mapea OrderMechanic con PK compuesta (un mecánico no se asigna dos veces a la misma orden)
            modelBuilder.Entity<OrderMechanic>(entity =>
            {
                entity.HasKey(e => new { e.EmployeeID, e.Folio });
                entity.HasOne(e => e.Mechanic)
                    .WithMany(m => m.OrderMechanics)
                    .HasForeignKey(e => e.EmployeeID);
                entity.HasOne(e => e.ServiceOrder)
                    .WithMany(s => s.OrderMechanics)
                    .HasForeignKey(e => e.Folio);
            });
        }
    }
}

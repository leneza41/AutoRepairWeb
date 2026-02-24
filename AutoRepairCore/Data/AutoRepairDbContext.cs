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

            // Cliente
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerID);
                entity.Property(e => e.RFC).IsRequired().HasMaxLength(13);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FirstLastname).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(50);
            });

            // Vehículo — los triggers calculan Antiquity en INSERT/UPDATE
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.SerialNumber);
                entity.Property(e => e.SerialNumber).HasMaxLength(50);
                entity.Property(e => e.PlateNumber).IsRequired().HasMaxLength(15);
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Vehicles)
                    .HasForeignKey(e => e.CustomerID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.ToTable(t =>
                {
                    t.HasTrigger("before_vehicle_insert");
                    t.HasTrigger("before_vehicle_update");
                });
            });

            // Mecánico
            modelBuilder.Entity<Mechanic>(entity =>
            {
                entity.HasKey(e => e.EmployeeID);
                entity.Property(e => e.RFC).IsRequired().HasMaxLength(13);
                entity.Property(e => e.Salary).HasColumnType("decimal(10,2)");
            });

            // Servicio
            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasKey(e => e.ServiceID);
                entity.Property(e => e.Cost).HasColumnType("decimal(10,2)");
            });

            // Refacción
            modelBuilder.Entity<Replacement>(entity =>
            {
                entity.HasKey(e => e.ReplacementID);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(10,2)");
            });

            // Orden de servicio — sin CASCADE para no borrar órdenes al cambiar vehículo
            modelBuilder.Entity<ServiceOrder>(entity =>
            {
                entity.HasKey(e => e.Folio);
                entity.Property(e => e.Cost).HasColumnType("decimal(12,2)");
                entity.HasOne(e => e.Vehicle)
                    .WithMany(v => v.ServiceOrders)
                    .HasForeignKey(e => e.SerialNumber)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Refacciones de la orden (PK compuesta)
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

            // Servicios de la orden (PK compuesta) — trigger recalcula el costo de la orden
            modelBuilder.Entity<OrderService>(entity =>
            {
                entity.HasKey(e => new { e.ServiceID, e.Folio });
                entity.HasOne(e => e.Service)
                    .WithMany(s => s.OrderServices)
                    .HasForeignKey(e => e.ServiceID);
                entity.HasOne(e => e.ServiceOrder)
                    .WithMany(s => s.OrderServices)
                    .HasForeignKey(e => e.Folio);
                entity.ToTable(t =>
                {
                    t.HasTrigger("trg_UpdateServiceOrderCost");
                });
            });

            // Mecánicos asignados a la orden (PK compuesta)
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

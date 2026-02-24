using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AutoRepairCore.Data;
using AutoRepairCore.Models;

namespace AutoRepairCore.Pages.ServiceOrders
{
    public class CreateModel : PageModel
    {
        private readonly AutoRepairDbContext _context;
        private readonly ILogger<CreateModel> _logger;
        private const decimal IVA_RATE = 0.16m;

        public CreateModel(AutoRepairDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty] public int SelectedCustomerID { get; set; }
        [BindProperty] public string SelectedSerialNumber { get; set; } = string.Empty;
        [BindProperty] public List<int> SelectedServiceIds { get; set; } = new();
        [BindProperty] public List<int> ServiceQuantities { get; set; } = new();

        public SelectList Customers { get; set; } = null!;
        public List<Service> AllServices { get; set; } = new();
        public DateTime CurrentDate { get; set; } = DateTime.Now;

        // Carga el formulario vacío
        public async Task<IActionResult> OnGetAsync(int? customerId)
        {
            await LoadFormDataAsync();
            CurrentDate = DateTime.Now;

            if (customerId.HasValue)
                SelectedCustomerID = customerId.Value;

            return Page();
        }

        // Guarda la nueva orden
        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("SelectedSerialNumber");
            ValidateForm();

            if (!ModelState.IsValid)
            {
                await LoadFormDataAsync();
                return Page();
            }

            try
            {
                // La transacción garantiza que la orden y sus servicios se guarden juntos o no se guarda nada
                await using var transaction = await _context.Database.BeginTransactionAsync();

                // Calcula el costo con IVA
                var services = await _context.Services
                    .Where(s => SelectedServiceIds.Contains(s.ServiceID))
                    .ToListAsync();

                decimal subtotal = SelectedServiceIds
                    .Select((id, i) => new { svc = services.FirstOrDefault(s => s.ServiceID == id), qty = i < ServiceQuantities.Count ? ServiceQuantities[i] : 1 })
                    .Where(x => x.svc != null)
                    .Sum(x => x.svc!.Cost * x.qty);

                // Crea la orden con fecha y folio automáticos
                var serviceOrder = new ServiceOrder
                {
                    SerialNumber = SelectedSerialNumber,
                    EntryDate = DateTime.Now,
                    EstimatedDeliveryTime = DateTime.Now.AddDays(1),
                    State = "Abierta",
                    Cost = subtotal * (1 + IVA_RATE)
                };

                _context.ServiceOrders.Add(serviceOrder);
                // Primer save: obtiene el Folio generado por SQL Server (necesario para los OrderService)
                await _context.SaveChangesAsync();

                // Agrega los servicios de la orden
                for (int i = 0; i < SelectedServiceIds.Count; i++)
                {
                    _context.OrderServices.Add(new OrderService
                    {
                        ServiceID = SelectedServiceIds[i],
                        Folio = serviceOrder.Folio,
                        Quantity = i < ServiceQuantities.Count ? ServiceQuantities[i] : 1
                    });
                }

                // Segundo save: inserta los servicios y activa el trigger de costo
                await _context.SaveChangesAsync();

                // Confirma ambos saves en la BD
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Orden de servicio #{serviceOrder.Folio} creada exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                // Si cualquier save falla, la transacción revierte ambos automáticamente
                _logger.LogError(ex, "Error al crear la orden de servicio");
                TempData["ErrorMessage"] = "Ocurrió un error al crear la orden. Por favor intente de nuevo.";
                await LoadFormDataAsync();
                return Page();
            }
        }

        // Devuelve los vehículos y datos del cliente seleccionado (AJAX)
        public async Task<JsonResult> OnGetVehiclesAsync(int customerId)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            var vehicles = await _context.Vehicles
                .Where(v => v.CustomerID == customerId)
                .Select(v => new
                {
                    serialNumber = v.SerialNumber,
                    display = $"{v.Brand} {v.Model} - {v.PlateNumber} ({v.Year})"
                })
                .ToListAsync();

            return new JsonResult(new
            {
                vehicles,
                customerRfc  = customer?.RFC ?? "",
                customerName = customer != null ? $"{customer.Name} {customer.FirstLastname}" : ""
            });
        }

        // Valida los campos del formulario
        private void ValidateForm()
        {
            if (SelectedCustomerID <= 0)
                ModelState.AddModelError("SelectedCustomerID", "Es necesario seleccionar un cliente.");

            if (string.IsNullOrEmpty(SelectedSerialNumber))
                ModelState.AddModelError("SelectedSerialNumber", "Es necesario seleccionar un vehículo.");

            if (SelectedServiceIds.Count == 0)
                ModelState.AddModelError("SelectedServiceIds", "Es necesario agregar al menos un servicio.");

            for (int i = 0; i < ServiceQuantities.Count; i++)
            {
                if (ServiceQuantities[i] < 1)
                    ModelState.AddModelError("ServiceQuantities", $"La cantidad del servicio #{i + 1} debe ser al menos 1.");
                if (ServiceQuantities[i] > 9999)
                    ModelState.AddModelError("ServiceQuantities", $"La cantidad del servicio #{i + 1} no puede superar 9,999.");
            }
        }

        // Carga clientes y servicios para el formulario
        private async Task LoadFormDataAsync()
        {
            var customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();
            Customers = new SelectList(customers, "CustomerID", "Name");
            AllServices = await _context.Services.OrderBy(s => s.Name).ToListAsync();
        }
    }
}

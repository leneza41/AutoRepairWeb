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
        private const decimal IVA_RATE = 0.16m;

        public CreateModel(AutoRepairDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int SelectedCustomerID { get; set; }

        [BindProperty]
        public string SelectedSerialNumber { get; set; } = string.Empty;

        [BindProperty]
        public List<int> SelectedServiceIds { get; set; } = new();

        [BindProperty]
        public List<int> ServiceQuantities { get; set; } = new();

        public SelectList Customers { get; set; } = null!;
        public List<Service> AllServices { get; set; } = new();
        public DateTime CurrentDate { get; set; } = DateTime.Now;

        public async Task<IActionResult> OnGetAsync(int? customerId)
        {
            await LoadCustomersAsync();
            await LoadServicesAsync();
            CurrentDate = DateTime.Now;

            if (customerId.HasValue)
                SelectedCustomerID = customerId.Value;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("SelectedSerialNumber");

            if (SelectedCustomerID <= 0)
                ModelState.AddModelError("SelectedCustomerID", "Es necesario seleccionar un cliente.");

            if (string.IsNullOrEmpty(SelectedSerialNumber))
                ModelState.AddModelError("SelectedSerialNumber", "Es necesario seleccionar un vehículo.");

            if (SelectedServiceIds.Count == 0)
                ModelState.AddModelError("SelectedServiceIds", "Es necesario agregar al menos un servicio.");

            // Validar cantidades
            for (int i = 0; i < ServiceQuantities.Count; i++)
            {
                if (ServiceQuantities[i] < 1)
                    ModelState.AddModelError("ServiceQuantities", $"La cantidad del servicio #{i + 1} debe ser al menos 1.");
                if (ServiceQuantities[i] > 9999)
                    ModelState.AddModelError("ServiceQuantities", $"La cantidad del servicio #{i + 1} no puede superar 9,999.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCustomersAsync();
                await LoadServicesAsync();
                return Page();
            }

            try
            {
                // Calculate cost from services
                var services = await _context.Services
                    .Where(s => SelectedServiceIds.Contains(s.ServiceID))
                    .ToListAsync();

                decimal subtotal = 0;
                for (int i = 0; i < SelectedServiceIds.Count; i++)
                {
                    var svc = services.FirstOrDefault(s => s.ServiceID == SelectedServiceIds[i]);
                    if (svc != null)
                    {
                        int qty = i < ServiceQuantities.Count ? ServiceQuantities[i] : 1;
                        subtotal += svc.Cost * qty;
                    }
                }
                decimal total = subtotal + (subtotal * IVA_RATE);

                var serviceOrder = new ServiceOrder
                {
                    SerialNumber = SelectedSerialNumber,
                    EntryDate = DateTime.Now,
                    EstimatedDeliveryTime = DateTime.Now.AddDays(1),
                    State = "Abierta",
                    Cost = total
                };

                _context.ServiceOrders.Add(serviceOrder);
                await _context.SaveChangesAsync();

                for (int i = 0; i < SelectedServiceIds.Count; i++)
                {
                    int qty = i < ServiceQuantities.Count ? ServiceQuantities[i] : 1;
                    _context.OrderServices.Add(new OrderService
                    {
                        ServiceID = SelectedServiceIds[i],
                        Folio = serviceOrder.Folio,
                        Quantity = qty
                    });
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Orden de servicio #{serviceOrder.Folio} creada exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al crear la orden: {ex.Message}";
                await LoadCustomersAsync();
                await LoadServicesAsync();
                return Page();
            }
        }

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
                customerRfc = customer != null ? customer.RFC : "",
                customerName = customer != null ? $"{customer.Name} {customer.FirstLastname}" : ""
            });
        }

        public async Task<JsonResult> OnGetServiceInfoAsync(int serviceId)
        {
            var svc = await _context.Services.FindAsync(serviceId);
            if (svc == null) return new JsonResult(null);
            return new JsonResult(new { svc.ServiceID, svc.Name, svc.Cost });
        }

        private async Task LoadCustomersAsync()
        {
            var customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();
            Customers = new SelectList(customers, "CustomerID", "Name");
        }

        private async Task LoadServicesAsync()
        {
            AllServices = await _context.Services.OrderBy(s => s.Name).ToListAsync();
        }
    }
}

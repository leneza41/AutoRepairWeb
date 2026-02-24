using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AutoRepairCore.Data;
using AutoRepairCore.Models;

namespace AutoRepairCore.Pages.ServiceOrders
{
    public class EditModel : PageModel
    {
        private readonly AutoRepairDbContext _context;
        private const decimal IVA_RATE = 0.16m;

        public EditModel(AutoRepairDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ServiceOrder ServiceOrder { get; set; } = null!;

        [BindProperty]
        public int SelectedCustomerID { get; set; }

        [BindProperty]
        public List<int> SelectedServiceIds { get; set; } = new();

        [BindProperty]
        public List<int> ServiceQuantities { get; set; } = new();

        public SelectList Customers { get; set; } = null!;
        public List<Vehicle> CustomerVehicles { get; set; } = new();
        public List<Service> AllServices { get; set; } = new();
        public List<OrderService> ExistingOrderServices { get; set; } = new();
        public string CustomerRfc { get; set; } = string.Empty;
        public string CustomerFullName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var serviceOrder = await _context.ServiceOrders
                .Include(s => s.Vehicle).ThenInclude(v => v.Customer)
                .Include(s => s.OrderServices).ThenInclude(os => os.Service)
                .FirstOrDefaultAsync(m => m.Folio == id);

            if (serviceOrder == null)
            {
                TempData["ErrorMessage"] = "No se encontró la orden de servicio.";
                return RedirectToPage("./Index");
            }

            ServiceOrder = serviceOrder;
            SelectedCustomerID = serviceOrder.Vehicle.CustomerID;
            ExistingOrderServices = serviceOrder.OrderServices.ToList();
            CustomerRfc = serviceOrder.Vehicle.Customer.RFC;
            CustomerFullName = $"{serviceOrder.Vehicle.Customer.Name} {serviceOrder.Vehicle.Customer.FirstLastname}";

            await LoadCustomersAsync();
            await LoadVehiclesForCustomerAsync(SelectedCustomerID);
            await LoadServicesAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("ServiceOrder.Vehicle");
            ModelState.Remove("ServiceOrder.OrderReplacements");
            ModelState.Remove("ServiceOrder.OrderServices");
            ModelState.Remove("ServiceOrder.OrderMechanics");

            if (SelectedServiceIds.Count == 0)
                ModelState.AddModelError("SelectedServiceIds", "Es necesario agregar al menos un servicio.");

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
                if (SelectedCustomerID > 0)
                {
                    await LoadVehiclesForCustomerAsync(SelectedCustomerID);
                    var customer = await _context.Customers.FindAsync(SelectedCustomerID);
                    if (customer != null)
                    {
                        CustomerRfc = customer.RFC;
                        CustomerFullName = $"{customer.Name} {customer.FirstLastname}";
                    }
                }
                return Page();
            }

            try
            {
                // Recalculate cost with IVA
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
                ServiceOrder.Cost = subtotal + (subtotal * IVA_RATE);

                _context.Attach(ServiceOrder).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Delete existing services first, save, then insert new ones
                var existingServices = await _context.OrderServices
                    .Where(os => os.Folio == ServiceOrder.Folio).ToListAsync();
                _context.OrderServices.RemoveRange(existingServices);
                await _context.SaveChangesAsync();

                for (int i = 0; i < SelectedServiceIds.Count; i++)
                {
                    _context.OrderServices.Add(new OrderService
                    {
                        ServiceID = SelectedServiceIds[i],
                        Folio = ServiceOrder.Folio,
                        Quantity = i < ServiceQuantities.Count ? ServiceQuantities[i] : 1
                    });
                }
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Orden de servicio #{ServiceOrder.Folio} actualizada exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ServiceOrderExists(ServiceOrder.Folio))
                {
                    TempData["ErrorMessage"] = "La orden de servicio ya no existe.";
                    return RedirectToPage("./Index");
                }
                throw;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar la orden: {ex.Message}";
                await LoadCustomersAsync();
                await LoadServicesAsync();
                if (SelectedCustomerID > 0)
                {
                    await LoadVehiclesForCustomerAsync(SelectedCustomerID);
                    var customer = await _context.Customers.FindAsync(SelectedCustomerID);
                    if (customer != null)
                    {
                        CustomerRfc = customer.RFC;
                        CustomerFullName = $"{customer.Name} {customer.FirstLastname}";
                    }
                }
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
                }).ToListAsync();

            return new JsonResult(new
            {
                vehicles,
                customerRfc = customer?.RFC ?? "",
                customerName = customer != null ? $"{customer.Name} {customer.FirstLastname}" : ""
            });
        }

        private bool ServiceOrderExists(int id) =>
            _context.ServiceOrders.Any(e => e.Folio == id);

        private async Task LoadCustomersAsync()
        {
            var customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();
            Customers = new SelectList(customers, "CustomerID", "Name");
        }

        private async Task LoadVehiclesForCustomerAsync(int customerId)
        {
            CustomerVehicles = await _context.Vehicles
                .Where(v => v.CustomerID == customerId).ToListAsync();
        }

        private async Task LoadServicesAsync()
        {
            AllServices = await _context.Services.OrderBy(s => s.Name).ToListAsync();
        }
    }
}

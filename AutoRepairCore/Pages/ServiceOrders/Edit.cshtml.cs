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

        public EditModel(AutoRepairDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ServiceOrder ServiceOrder { get; set; } = null!;

        [BindProperty]
        public int SelectedCustomerID { get; set; }

        [BindProperty]
        public List<int> SelectedMechanicIds { get; set; } = new List<int>();

        [BindProperty]
        public List<int> SelectedServiceIds { get; set; } = new List<int>();

        [BindProperty]
        public List<int> ServiceQuantities { get; set; } = new List<int>();

        public SelectList Customers { get; set; } = null!;
        public List<Vehicle> CustomerVehicles { get; set; } = new List<Vehicle>();
        public List<Mechanic> AllMechanics { get; set; } = new List<Mechanic>();
        public List<Service> AllServices { get; set; } = new List<Service>();
        public List<OrderService> ExistingOrderServices { get; set; } = new List<OrderService>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceOrder = await _context.ServiceOrders
                .Include(s => s.Vehicle)
                .Include(s => s.OrderMechanics)
                .Include(s => s.OrderServices)
                .FirstOrDefaultAsync(m => m.Folio == id);

            if (serviceOrder == null)
            {
                TempData["ErrorMessage"] = "No se encontró la orden de servicio.";
                return RedirectToPage("./Index");
            }

            ServiceOrder = serviceOrder;
            SelectedCustomerID = serviceOrder.Vehicle.CustomerID;

            // Load existing mechanics and services
            SelectedMechanicIds = serviceOrder.OrderMechanics.Select(om => om.EmployeeID).ToList();
            ExistingOrderServices = serviceOrder.OrderServices.ToList();

            await LoadCustomersAsync();
            await LoadVehiclesForCustomerAsync(SelectedCustomerID);
            await LoadMechanicsAsync();
            await LoadServicesAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCustomersAsync();
                await LoadMechanicsAsync();
                await LoadServicesAsync();
                if (SelectedCustomerID > 0)
                {
                    await LoadVehiclesForCustomerAsync(SelectedCustomerID);
                }
                TempData["ErrorMessage"] = "Por favor, corrija los errores en el formulario.";
                return Page();
            }

            _context.Attach(ServiceOrder).State = EntityState.Modified;

            try
            {
                // Remove existing mechanics
                var existingMechanics = await _context.OrderMechanics
                    .Where(om => om.Folio == ServiceOrder.Folio)
                    .ToListAsync();
                _context.OrderMechanics.RemoveRange(existingMechanics);

                // Add new mechanics
                foreach (var mechanicId in SelectedMechanicIds)
                {
                    var orderMechanic = new OrderMechanic
                    {
                        EmployeeID = mechanicId,
                        Folio = ServiceOrder.Folio
                    };
                    _context.OrderMechanics.Add(orderMechanic);
                }

                // Remove existing services
                var existingServices = await _context.OrderServices
                    .Where(os => os.Folio == ServiceOrder.Folio)
                    .ToListAsync();
                _context.OrderServices.RemoveRange(existingServices);

                // Add new services
                for (int i = 0; i < SelectedServiceIds.Count; i++)
                {
                    var serviceId = SelectedServiceIds[i];
                    var quantity = i < ServiceQuantities.Count ? ServiceQuantities[i] : 1;

                    var orderService = new OrderService
                    {
                        ServiceID = serviceId,
                        Folio = ServiceOrder.Folio,
                        Quantity = quantity
                    };
                    _context.OrderServices.Add(orderService);
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
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al actualizar la orden: {ex.Message}";
                await LoadCustomersAsync();
                await LoadMechanicsAsync();
                await LoadServicesAsync();
                if (SelectedCustomerID > 0)
                {
                    await LoadVehiclesForCustomerAsync(SelectedCustomerID);
                }
                return Page();
            }
        }

        public async Task<JsonResult> OnGetVehiclesAsync(int customerId)
        {
            var vehicles = await _context.Vehicles
                .Where(v => v.CustomerID == customerId)
                .Select(v => new
                {
                    serialNumber = v.SerialNumber,
                    display = $"{v.Brand} {v.Model} - {v.PlateNumber} ({v.Year})"
                })
                .ToListAsync();

            return new JsonResult(vehicles);
        }

        private bool ServiceOrderExists(int id)
        {
            return _context.ServiceOrders.Any(e => e.Folio == id);
        }

        private async Task LoadCustomersAsync()
        {
            var customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            Customers = new SelectList(customers, "CustomerID", "Name");
        }

        private async Task LoadVehiclesForCustomerAsync(int customerId)
        {
            CustomerVehicles = await _context.Vehicles
                .Where(v => v.CustomerID == customerId)
                .ToListAsync();
        }

        private async Task LoadMechanicsAsync()
        {
            AllMechanics = await _context.Mechanics
                .OrderBy(m => m.Name)
                .ThenBy(m => m.FirstLastname)
                .ToListAsync();
        }

        private async Task LoadServicesAsync()
        {
            AllServices = await _context.Services
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}

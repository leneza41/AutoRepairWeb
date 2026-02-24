using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AutoRepairCore.Data;
using AutoRepairCore.Models;
using Microsoft.Extensions.Logging;

namespace AutoRepairCore.Pages.ServiceOrders
{
    public class EditModel : PageModel
    {
        private readonly AutoRepairDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(AutoRepairDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
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

        // Carga la orden existente para editar
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

            await LoadFormDataAsync(SelectedCustomerID);
            return Page();
        }

        // Guarda los cambios de la orden
        public async Task<IActionResult> OnPostAsync()
        {
            // Excluye campos que no vienen del formulario
            ModelState.Remove("ServiceOrder.Vehicle");
            ModelState.Remove("ServiceOrder.OrderReplacements");
            ModelState.Remove("ServiceOrder.OrderServices");
            ModelState.Remove("ServiceOrder.OrderMechanics");
            ModelState.Remove("ServiceOrder.EntryDate");
            ModelState.Remove("ServiceOrder.EstimatedDeliveryTime");
            ModelState.Remove("ServiceOrder.DeliveryTime");
            ModelState.Remove("ServiceOrder.Cost");

            ValidateForm();

            if (!ModelState.IsValid)
            {
                await LoadFormDataAsync(SelectedCustomerID);
                await LoadCustomerInfoAsync(SelectedCustomerID);
                return Page();
            }

            try
            {
                // Reemplaza los servicios para que el trigger recalcule el costo
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

                // Carga la orden desde la BD para no pisar fechas ni costo
                var dbOrder = await _context.ServiceOrders
                    .FirstOrDefaultAsync(so => so.Folio == ServiceOrder.Folio);

                if (dbOrder == null)
                {
                    TempData["ErrorMessage"] = "La orden de servicio ya no existe.";
                    return RedirectToPage("./Index");
                }

                // Solo actualiza los campos editables por el usuario
                dbOrder.State = ServiceOrder.State;
                dbOrder.SerialNumber = ServiceOrder.SerialNumber;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Orden de servicio #{ServiceOrder.Folio} actualizada exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Conflicto de concurrencia al editar la orden {Folio}", ServiceOrder.Folio);
                if (!ServiceOrderExists(ServiceOrder.Folio))
                {
                    TempData["ErrorMessage"] = "La orden de servicio ya no existe.";
                    return RedirectToPage("./Index");
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar la orden {Folio}", ServiceOrder.Folio);
                TempData["ErrorMessage"] = "Ocurrió un error al actualizar la orden. Por favor intente de nuevo.";
                await LoadFormDataAsync(SelectedCustomerID);
                await LoadCustomerInfoAsync(SelectedCustomerID);
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
                }).ToListAsync();

            return new JsonResult(new
            {
                vehicles,
                customerRfc  = customer?.RFC ?? "",
                customerName = customer != null ? $"{customer.Name} {customer.FirstLastname}" : ""
            });
        }

        // Verifica si la orden existe en la BD
        private bool ServiceOrderExists(int id) =>
            _context.ServiceOrders.Any(e => e.Folio == id);

        // Valida los campos del formulario
        private void ValidateForm()
        {
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

        // Carga clientes, vehículos del cliente y servicios para el formulario
        private async Task LoadFormDataAsync(int customerId = 0)
        {
            var customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();
            Customers = new SelectList(customers, "CustomerID", "Name");
            AllServices = await _context.Services.OrderBy(s => s.Name).ToListAsync();

            if (customerId > 0)
                CustomerVehicles = await _context.Vehicles
                    .Where(v => v.CustomerID == customerId).ToListAsync();
        }

        // Carga el RFC y nombre del cliente seleccionado
        private async Task LoadCustomerInfoAsync(int customerId)
        {
            if (customerId <= 0) return;
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) return;
            CustomerRfc = customer.RFC;
            CustomerFullName = $"{customer.Name} {customer.FirstLastname}";
        }
    }
}

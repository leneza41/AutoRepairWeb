using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AutoRepairCore.Data;
using AutoRepairCore.Models;
using Microsoft.Extensions.Logging;

namespace AutoRepairCore.Pages.ServiceOrders
{
    public class DeleteModel : PageModel
    {
        private readonly AutoRepairDbContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(AutoRepairDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public ServiceOrder ServiceOrder { get; set; } = null!;

        // Carga la orden para mostrar la confirmación
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var serviceOrder = await _context.ServiceOrders
                .Include(s => s.Vehicle).ThenInclude(v => v.Customer)
                .FirstOrDefaultAsync(m => m.Folio == id);

            if (serviceOrder == null)
            {
                TempData["ErrorMessage"] = "No se encontró la orden de servicio.";
                return RedirectToPage("./Index");
            }

            ServiceOrder = serviceOrder;
            return Page();
        }

        // Elimina la orden y sus registros hijos
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var serviceOrder = await _context.ServiceOrders.FindAsync(id);

            if (serviceOrder == null)
            {
                TempData["ErrorMessage"] = "La orden de servicio ya no existe.";
                return RedirectToPage("./Index");
            }

            try
            {
                // Elimina hijos primero para evitar errores de FK
                _context.OrderServices.RemoveRange(
                    await _context.OrderServices.Where(os => os.Folio == id).ToListAsync());

                _context.OrderReplacements.RemoveRange(
                    await _context.OrderReplacements.Where(or => or.Folio == id).ToListAsync());

                _context.OrderMechanics.RemoveRange(
                    await _context.OrderMechanics.Where(om => om.Folio == id).ToListAsync());

                await _context.SaveChangesAsync();

                // Elimina la orden
                _context.ServiceOrders.Remove(serviceOrder);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Orden de servicio #{id} eliminada exitosamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la orden {Id}", id);
                TempData["ErrorMessage"] = "Ocurrió un error al eliminar la orden. Por favor intente de nuevo.";
                return RedirectToPage("./Details", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}

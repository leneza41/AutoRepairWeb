using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AutoRepairCore.Data;
using AutoRepairCore.Models;

namespace AutoRepairCore.Pages.ServiceOrders
{
    public class DeleteModel : PageModel
    {
        private readonly AutoRepairDbContext _context;

        public DeleteModel(AutoRepairDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ServiceOrder ServiceOrder { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceOrder = await _context.ServiceOrders
                .Include(s => s.Vehicle)
                    .ThenInclude(v => v.Customer)
                .FirstOrDefaultAsync(m => m.Folio == id);

            if (serviceOrder == null)
            {
                TempData["ErrorMessage"] = "No se encontró la orden de servicio.";
                return RedirectToPage("./Index");
            }

            ServiceOrder = serviceOrder;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceOrder = await _context.ServiceOrders.FindAsync(id);

            if (serviceOrder != null)
            {
                try
                {
                    ServiceOrder = serviceOrder;
                    _context.ServiceOrders.Remove(ServiceOrder);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Orden de servicio #{id} eliminada exitosamente.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error al eliminar la orden: {ex.Message}";
                    return RedirectToPage("./Details", new { id });
                }
            }
            else
            {
                TempData["ErrorMessage"] = "La orden de servicio ya no existe.";
            }

            return RedirectToPage("./Index");
        }
    }
}

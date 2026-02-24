using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AutoRepairCore.Data;
using AutoRepairCore.Models;

namespace AutoRepairCore.Pages.ServiceOrders
{
    public class DetailsModel : PageModel
    {
        private readonly AutoRepairDbContext _context;

        public DetailsModel(AutoRepairDbContext context)
        {
            _context = context;
        }

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
                .Include(s => s.OrderMechanics)
                    .ThenInclude(om => om.Mechanic)
                .Include(s => s.OrderServices)
                    .ThenInclude(os => os.Service)
                .FirstOrDefaultAsync(m => m.Folio == id);

            if (serviceOrder == null)
            {
                return NotFound();
            }

            ServiceOrder = serviceOrder;
            return Page();
        }
    }
}

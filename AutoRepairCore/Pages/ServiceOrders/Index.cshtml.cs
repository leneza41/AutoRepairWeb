using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AutoRepairCore.Data;
using AutoRepairCore.Models;

namespace AutoRepairCore.Pages.ServiceOrders
{
    public class IndexModel : PageModel
    {
        private readonly AutoRepairDbContext _context;

        public IndexModel(AutoRepairDbContext context)
        {
            _context = context;
        }

        public IList<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();

        public async Task OnGetAsync()
        {
            ServiceOrders = await _context.ServiceOrders
                .Include(s => s.Vehicle)
                    .ThenInclude(v => v.Customer)
                .Include(s => s.OrderServices)
                    .ThenInclude(os => os.Service)
                .OrderByDescending(s => s.Folio)
                .ToListAsync();
        }
    }
}

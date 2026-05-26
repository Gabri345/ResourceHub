using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ResourceHub.Data;
using ResourceHub.Models;

namespace ResourceHub.Pages.Resources
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Resource> Resource { get; set; } = new List<Resource>();

        public string? SearchTerm { get; set; }

        public string? Category { get; set; }

        public async Task OnGetAsync(string? searchTerm, string? category)
        {
            SearchTerm = searchTerm;
            Category = category;

            var query = _context.Resources
                .Include(r => r.Ratings)
                .Include(r => r.Reports)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r =>
                    r.Title.Contains(searchTerm) ||
                    r.Description.Contains(searchTerm) ||
                    r.Category.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(r => r.Category == category);
            }

            Resource = await query
                .OrderByDescending(r => r.UploadDate)
                .ToListAsync();
        }
    }
}

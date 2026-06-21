using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
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

        public IReadOnlyList<string> Categories { get; } = ResourceCategories.SchoolSubjects;

        public HashSet<int> FavoriteResourceIds { get; set; } = new();

        public async Task OnGetAsync(string? searchTerm, string? category)
        {
            SearchTerm = searchTerm;
            Category = category;

            var userId = CurrentUserId();

            var query = _context.Resources
                .Include(r => r.Ratings)
                .Include(r => r.Reports)
                .Include(r => r.Shares)
                .Where(r => !r.IsPrivate || r.UploaderId == userId || r.Shares.Any(s => s.SharedWithUserId == userId))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r =>
                    r.Title.Contains(searchTerm) ||
                    r.Description.Contains(searchTerm) ||
                    r.Category.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(category) && Categories.Contains(category))
            {
                query = query.Where(r => r.Category == category);
            }

            Resource = await query
                .OrderByDescending(r => r.UploadDate)
                .ToListAsync();

            if (User.Identity?.IsAuthenticated == true)
            {
                FavoriteResourceIds = await _context.ResourceFavorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.ResourceId)
                    .ToHashSetAsync();
            }
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync(int id, string? searchTerm, string? category)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                var returnUrl = Url.Page("./Index", new { searchTerm, category }) ?? "/Resources";
                return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl });
            }

            var userId = CurrentUserId();
            var favorite = await _context.ResourceFavorites
                .FirstOrDefaultAsync(f => f.ResourceId == id && f.UserId == userId);

            if (favorite is null)
            {
                var resourceExists = await _context.Resources.AnyAsync(r => r.Id == id);
                if (resourceExists)
                {
                    _context.ResourceFavorites.Add(new ResourceFavorite
                    {
                        ResourceId = id,
                        UserId = userId
                    });
                }
            }
            else
            {
                _context.ResourceFavorites.Remove(favorite);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { searchTerm, category });
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ResourceHub.Data;
using ResourceHub.Models;

namespace ResourceHub.Pages.Resources
{
    [Authorize]
    public class FavoritesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public FavoritesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Resource> Resources { get; set; } = new List<Resource>();

        public async Task OnGetAsync()
        {
            var userId = CurrentUserId();
            Resources = await _context.ResourceFavorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Resource)
                    .ThenInclude(r => r.Ratings)
                .Include(f => f.Resource)
                    .ThenInclude(r => r.Reports)
                .OrderByDescending(f => f.CreatedOn)
                .Select(f => f.Resource)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostRemoveAsync(int id)
        {
            var userId = CurrentUserId();
            var favorite = await _context.ResourceFavorites
                .FirstOrDefaultAsync(f => f.ResourceId == id && f.UserId == userId);

            if (favorite is not null)
            {
                _context.ResourceFavorites.Remove(favorite);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }
    }
}

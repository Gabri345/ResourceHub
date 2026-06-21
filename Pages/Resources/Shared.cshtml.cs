using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ResourceHub.Data;
using ResourceHub.Models;

namespace ResourceHub.Pages.Resources
{
    [Authorize]
    public class SharedModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SharedModel(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Resources shared with the current user, keyed by folder name.
        /// </summary>
        public Dictionary<string, List<Resource>> ResourcesByFolder { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Load own private resources + resources shared with the user
            var ownPrivate = await _context.Resources
                .Include(r => r.Ratings)
                .Include(r => r.Reports)
                .Where(r => r.IsPrivate && r.UploaderId == userId)
                .ToListAsync();

            var sharedWithMe = await _context.ResourceShares
                .Where(s => s.SharedWithUserId == userId)
                .Include(s => s.Resource)
                    !.ThenInclude(r => r!.Ratings)
                .Include(s => s.Resource)
                    !.ThenInclude(r => r!.Reports)
                .Where(s => s.Resource != null)
                .Select(s => s.Resource!)
                .ToListAsync();

            // Merge, deduplicate by Id, sort by folder then date
            var all = ownPrivate
                .Concat(sharedWithMe)
                .DistinctBy(r => r.Id)
                .OrderBy(r => r.Folder)
                .ThenByDescending(r => r.UploadDate)
                .ToList();

            ResourcesByFolder = all
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Folder) ? "General" : r.Folder)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }
}

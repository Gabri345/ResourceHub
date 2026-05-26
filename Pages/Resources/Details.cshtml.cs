using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ResourceHub.Data;
using ResourceHub.Models;

namespace ResourceHub.Pages.Resources
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Resource Resource { get; set; } = default!;

        [BindProperty]
        [StringLength(700)]
        public string? NewComment { get; set; }

        [BindProperty]
        [Range(1, 5)]
        public int RatingValue { get; set; } = 5;

        [BindProperty]
        [StringLength(500)]
        public string? ReportReason { get; set; }

        public double? AverageRating { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaded = await LoadResourceAsync(id.Value);
            return loaded ? Page() : NotFound();
        }

        public async Task<IActionResult> OnPostCommentAsync(int id)
        {
            if (!string.IsNullOrWhiteSpace(NewComment))
            {
                _context.ResourceComments.Add(new ResourceComment
                {
                    ResourceId = id,
                    Content = NewComment.Trim(),
                    UserId = CurrentUserId()
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRatingAsync(int id)
        {
            if (RatingValue is >= 1 and <= 5)
            {
                _context.ResourceRatings.Add(new ResourceRating
                {
                    ResourceId = id,
                    Value = RatingValue,
                    UserId = CurrentUserId()
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostReportAsync(int id)
        {
            if (!string.IsNullOrWhiteSpace(ReportReason))
            {
                _context.ResourceReports.Add(new ResourceReport
                {
                    ResourceId = id,
                    Reason = ReportReason.Trim(),
                    UserId = CurrentUserId()
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id });
        }

        private async Task<bool> LoadResourceAsync(int id)
        {
            var resource = await _context.Resources
                .Include(r => r.Comments.OrderByDescending(c => c.CreatedOn))
                .Include(r => r.Ratings)
                .Include(r => r.Reports.OrderByDescending(rp => rp.CreatedOn))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (resource is null)
            {
                return false;
            }

            Resource = resource;
            AverageRating = resource.Ratings.Any() ? resource.Ratings.Average(r => r.Value) : null;
            return true;
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
        }
    }
}

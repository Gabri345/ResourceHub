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
        private readonly IWebHostEnvironment _environment;

        public DetailsModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

        public ResourceRating? CurrentUserRating { get; set; }

        public bool CurrentUserHasReported { get; set; }

        public bool CanDeleteResource { get; set; }

        public bool IsFavorite { get; set; }

        public string PreviewKind { get; set; } = "none";

        public string? PreviewText { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

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
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToLogin(id);
            }

            if (RatingValue is >= 1 and <= 5)
            {
                var userId = CurrentUserId();
                var existingRating = await _context.ResourceRatings
                    .FirstOrDefaultAsync(r => r.ResourceId == id && r.UserId == userId);

                if (existingRating is null)
                {
                    _context.ResourceRatings.Add(new ResourceRating
                    {
                        ResourceId = id,
                        Value = RatingValue,
                        UserId = userId
                    });

                    StatusMessage = "Your rating was saved.";
                }
                else
                {
                    existingRating.Value = RatingValue;
                    StatusMessage = "Your existing rating was updated.";
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostReportAsync(int id)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToLogin(id);
            }

            if (!string.IsNullOrWhiteSpace(ReportReason))
            {
                var userId = CurrentUserId();
                var alreadyReported = await _context.ResourceReports
                    .AnyAsync(r => r.ResourceId == id && r.UserId == userId);

                if (alreadyReported)
                {
                    StatusMessage = "You have already reported this resource.";
                    return RedirectToPage(new { id });
                }

                _context.ResourceReports.Add(new ResourceReport
                {
                    ResourceId = id,
                    Reason = ReportReason.Trim(),
                    UserId = userId
                });

                await _context.SaveChangesAsync();
                StatusMessage = "Your report was sent.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync(int id)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToLogin(id);
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

                    StatusMessage = "Resource saved to your favorites.";
                }
            }
            else
            {
                _context.ResourceFavorites.Remove(favorite);
                StatusMessage = "Resource removed from your favorites.";
            }

            await _context.SaveChangesAsync();
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
            await LoadPreviewAsync(resource);

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = CurrentUserId();
                CurrentUserRating = resource.Ratings.FirstOrDefault(r => r.UserId == userId);
                CurrentUserHasReported = resource.Reports.Any(r => r.UserId == userId);
                CanDeleteResource = resource.UploaderId == userId;
                IsFavorite = await _context.ResourceFavorites
                    .AnyAsync(f => f.ResourceId == id && f.UserId == userId);
                RatingValue = CurrentUserRating?.Value ?? 5;
            }

            return true;
        }

        private async Task LoadPreviewAsync(Resource resource)
        {
            if (string.IsNullOrWhiteSpace(resource.FilePath))
            {
                PreviewKind = "none";
                return;
            }

            var extension = Path.GetExtension(resource.FileName).ToLowerInvariant();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
            var textExtensions = new[] { ".txt", ".csv", ".json", ".xml", ".html", ".css", ".js", ".cs", ".md" };

            if (imageExtensions.Contains(extension))
            {
                PreviewKind = "image";
                return;
            }

            if (extension == ".pdf")
            {
                PreviewKind = "pdf";
                return;
            }

            if (!textExtensions.Contains(extension))
            {
                PreviewKind = "unsupported";
                return;
            }

            var fileName = Path.GetFileName(resource.FilePath);
            var fullPath = Path.Combine(_environment.WebRootPath, "uploads", fileName);

            if (!System.IO.File.Exists(fullPath))
            {
                PreviewKind = "unsupported";
                return;
            }

            PreviewKind = "text";
            var text = await System.IO.File.ReadAllTextAsync(fullPath);
            PreviewText = text.Length > 8000 ? text[..8000] + Environment.NewLine + "... preview truncated ..." : text;
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
        }

        private IActionResult RedirectToLogin(int id)
        {
            var returnUrl = Url.Page("./Details", new { id }) ?? $"/Resources/Details/{id}";
            return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl });
        }
    }
}

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<IdentityUser> _userManager;

        public DetailsModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
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

        [BindProperty]
        [Required]
        [EmailAddress]
        public string SharedEmail { get; set; } = string.Empty;

        public List<SharedUserViewModel> SharedUsers { get; set; } = new();

        public double? AverageRating { get; set; }

        public ResourceRating? CurrentUserRating { get; set; }

        public bool CurrentUserHasReported { get; set; }

        public bool CanDeleteResource { get; set; }

        public bool IsFavorite { get; set; }

        public string PreviewKind { get; set; } = "none";

        public string? PreviewText { get; set; }

        public string FileSize { get; set; } = "";

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetDownloadAsync(int id)
        {
            var resource = await _context.Resources
                .Include(r => r.Shares)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resource == null || string.IsNullOrEmpty(resource.FilePath))
            {
                return NotFound();
            }

            if (!CanAccess(resource))
            {
                return User.Identity?.IsAuthenticated == true ? Forbid() : RedirectToLogin(id);
            }

            var fileName = Path.GetFileName(resource.FilePath);
            var fullPath = Path.Combine(_environment.WebRootPath, "uploads", fileName);

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            var contentType = "application/octet-stream";

            return File(bytes, contentType, resource.FileName ?? fileName);
        }

        public async Task<IActionResult> OnPostAddShareAsync(int id)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToLogin(id);
            }

            var resource = await FindOwnedPrivateResourceAsync(id);
            if (resource is null)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                StatusMessage = "Enter a valid email address.";
                return RedirectToPage(new { id });
            }

            var targetUser = await _userManager.FindByEmailAsync(SharedEmail.Trim());
            if (targetUser is null)
            {
                StatusMessage = "No registered user was found with that email address.";
                return RedirectToPage(new { id });
            }

            if (targetUser.Id == resource.UploaderId)
            {
                StatusMessage = "You already own this resource.";
                return RedirectToPage(new { id });
            }

            var alreadyShared = await _context.ResourceShares
                .AnyAsync(s => s.ResourceId == id && s.SharedWithUserId == targetUser.Id);

            if (alreadyShared)
            {
                StatusMessage = "This user already has access.";
                return RedirectToPage(new { id });
            }

            _context.ResourceShares.Add(new ResourceShare
            {
                ResourceId = id,
                SharedWithUserId = targetUser.Id
            });

            await _context.SaveChangesAsync();
            StatusMessage = $"Access granted to {targetUser.Email}.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRemoveShareAsync(int id, int shareId)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToLogin(id);
            }

            var resource = await FindOwnedPrivateResourceAsync(id);
            if (resource is null)
            {
                return Forbid();
            }

            var share = await _context.ResourceShares
                .FirstOrDefaultAsync(s => s.Id == shareId && s.ResourceId == resource.Id);

            if (share is null)
            {
                StatusMessage = "That access entry no longer exists.";
                return RedirectToPage(new { id });
            }

            _context.ResourceShares.Remove(share);
            await _context.SaveChangesAsync();
            StatusMessage = "Access removed.";
            return RedirectToPage(new { id });
        }

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
            var userId = CurrentUserId();
            var resource = await _context.Resources
                .Include(r => r.Comments.OrderByDescending(c => c.CreatedOn))
                .Include(r => r.Ratings)
                .Include(r => r.Reports.OrderByDescending(rp => rp.CreatedOn))
                .Include(r => r.Shares)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (resource is null)
            {
                return false;
            }

            if (resource.IsPrivate && resource.UploaderId != userId && !resource.Shares.Any(s => s.SharedWithUserId == userId))
            {
                return false;
            }

            Resource = resource;
            AverageRating = resource.Ratings.Any() ? resource.Ratings.Average(r => r.Value) : null;
            await LoadPreviewAsync(resource);

            if (User.Identity?.IsAuthenticated == true)
            {
                CurrentUserRating = resource.Ratings.FirstOrDefault(r => r.UserId == userId);
                CurrentUserHasReported = resource.Reports.Any(r => r.UserId == userId);
                CanDeleteResource = resource.UploaderId == userId;
                IsFavorite = await _context.ResourceFavorites
                    .AnyAsync(f => f.ResourceId == id && f.UserId == userId);
                RatingValue = CurrentUserRating?.Value ?? 5;
            }

            if (CanDeleteResource && resource.IsPrivate && resource.Shares.Count > 0)
            {
                var sharedUserIds = resource.Shares.Select(s => s.SharedWithUserId).ToList();
                var emailsById = await _userManager.Users
                    .Where(u => sharedUserIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? "Unknown user");

                SharedUsers = resource.Shares
                    .Select(s => new SharedUserViewModel
                    {
                        ShareId = s.Id,
                        Email = emailsById.GetValueOrDefault(s.SharedWithUserId, "Unknown user")
                    })
                    .OrderBy(s => s.Email)
                    .ToList();
            }

            return true;
        }

        private async Task<Resource?> FindOwnedPrivateResourceAsync(int id)
        {
            var userId = CurrentUserId();
            return await _context.Resources
                .FirstOrDefaultAsync(r => r.Id == id && r.IsPrivate && r.UploaderId == userId);
        }

        private bool CanAccess(Resource resource)
        {
            if (!resource.IsPrivate)
            {
                return true;
            }

            if (User.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            var userId = CurrentUserId();
            return resource.UploaderId == userId ||
                   resource.Shares.Any(s => s.SharedWithUserId == userId);
        }

        private async Task LoadPreviewAsync(Resource resource)
        {
            if (string.IsNullOrWhiteSpace(resource.FilePath))
            {
                PreviewKind = "none";
                return;
            }

            var fileName = Path.GetFileName(resource.FilePath);
            var fullPath = Path.Combine(_environment.WebRootPath, "uploads", fileName);

            if (System.IO.File.Exists(fullPath))
            {
                var fileInfo = new FileInfo(fullPath);
                FileSize = FormatBytes(fileInfo.Length);
            }

            var extension = Path.GetExtension(resource.FileName ?? fileName).ToLowerInvariant();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" };
            var textExtensions = new[] { ".txt", ".csv", ".json", ".xml", ".html", ".css", ".js", ".cs", ".md", ".sql", ".yaml", ".yml" };
            var audioExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac" };
            var videoExtensions = new[] { ".mp4", ".webm", ".ogg", ".mov" };

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

            if (audioExtensions.Contains(extension))
            {
                PreviewKind = "audio";
                return;
            }

            if (videoExtensions.Contains(extension))
            {
                PreviewKind = "video";
                return;
            }

            if (!textExtensions.Contains(extension))
            {
                PreviewKind = "unsupported";
                return;
            }

            if (!System.IO.File.Exists(fullPath))
            {
                PreviewKind = "unsupported";
                return;
            }

            PreviewKind = "text";
            var text = await System.IO.File.ReadAllTextAsync(fullPath);
            PreviewText = text.Length > 8000 ? text[..8000] + Environment.NewLine + "... preview truncated ..." : text;
        }

        private string FormatBytes(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }
            return $"{dblSByte:0.##} {suffix[i]}";
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

        public class SharedUserViewModel
        {
            public int ShareId { get; set; }

            public string Email { get; set; } = string.Empty;
        }
    }
}

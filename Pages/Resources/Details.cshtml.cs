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

        public string PreviewKind { get; set; } = "none";

        public string? PreviewText { get; set; }

        public string FileExtension { get; set; } = string.Empty;

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
            await PreparePreviewAsync(resource);
            return true;
        }

        private async Task PreparePreviewAsync(Resource resource)
        {
            if (string.IsNullOrWhiteSpace(resource.FilePath))
            {
                PreviewKind = "none";
                return;
            }

            FileExtension = Path.GetExtension(resource.FileName).ToLowerInvariant();

            if (IsImage(FileExtension))
            {
                PreviewKind = "image";
                return;
            }

            if (FileExtension == ".pdf")
            {
                PreviewKind = "pdf";
                return;
            }

            if (IsTextFile(FileExtension))
            {
                PreviewKind = "text";
                PreviewText = await ReadPreviewTextAsync(resource.FilePath);
                return;
            }

            PreviewKind = "unsupported";
        }

        private async Task<string> ReadPreviewTextAsync(string filePath)
        {
            var relativePath = filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, relativePath));
            var webRoot = Path.GetFullPath(_environment.WebRootPath);

            if (!fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
            {
                return "File preview is not available.";
            }

            const int maxPreviewBytes = 200 * 1024;
            await using var stream = System.IO.File.OpenRead(fullPath);
            using var reader = new StreamReader(stream);
            var buffer = new char[maxPreviewBytes];
            var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
            var text = new string(buffer, 0, read);

            return stream.Length > maxPreviewBytes
                ? text + Environment.NewLine + Environment.NewLine + "... Preview shortened because the file is large."
                : text;
        }

        private static bool IsImage(string extension)
        {
            return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp";
        }

        private static bool IsTextFile(string extension)
        {
            return extension is ".txt" or ".cs" or ".html" or ".htm" or ".css" or ".js" or ".json" or ".xml" or ".md" or ".csv" or ".sql" or ".py" or ".java" or ".cpp" or ".c" or ".h";
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
        }
    }
}

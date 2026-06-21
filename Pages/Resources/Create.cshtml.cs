using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ResourceHub.Data;
using ResourceHub.Models;

namespace ResourceHub.Pages.Resources
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateModel(ApplicationDbContext context, IWebHostEnvironment environment, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        [BindProperty]
        public Resource Resource { get; set; } = new();

        [BindProperty]
        public IFormFile? UploadFile { get; set; }

        [BindProperty]
        public List<string> SharedEmails { get; set; } = new();

        public IReadOnlyList<string> Categories { get; } = ResourceCategories.SchoolSubjects;

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (UploadFile is not null && UploadFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var safeFileName = Path.GetFileName(UploadFile.FileName);
                var storedFileName = $"{Guid.NewGuid()}_{safeFileName}";
                var filePath = Path.Combine(uploadsFolder, storedFileName);

                await using var stream = System.IO.File.Create(filePath);
                await UploadFile.CopyToAsync(stream);

                Resource.FileName = safeFileName;
                Resource.FilePath = $"/uploads/{storedFileName}";
            }

            Resource.UploadDate = DateTime.Now;
            Resource.UploaderId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Ensure folder has a default value when public
            if (!Resource.IsPrivate || string.IsNullOrWhiteSpace(Resource.Folder))
            {
                Resource.Folder = "General";
            }

            _context.Resources.Add(Resource);
            await _context.SaveChangesAsync();

            // Handle sharing with users by email (only for private resources)
            if (Resource.IsPrivate && SharedEmails.Count > 0)
            {
                foreach (var email in SharedEmails.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct())
                {
                    var targetUser = await _userManager.FindByEmailAsync(email.Trim());
                    if (targetUser != null && targetUser.Id != Resource.UploaderId)
                    {
                        var alreadyShared = await _context.ResourceShares
                            .AnyAsync(s => s.ResourceId == Resource.Id && s.SharedWithUserId == targetUser.Id);

                        if (!alreadyShared)
                        {
                            _context.ResourceShares.Add(new ResourceShare
                            {
                                ResourceId = Resource.Id,
                                SharedWithUserId = targetUser.Id
                            });
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Details", new { id = Resource.Id });
        }
    }
}

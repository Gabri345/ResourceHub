using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ResourceHub.Data;
using ResourceHub.Models;

namespace ResourceHub.Pages.Resources
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CreateModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public Resource Resource { get; set; } = new();

        [BindProperty]
        public IFormFile? UploadFile { get; set; }

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
            Resource.UploaderId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";

            _context.Resources.Add(Resource);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = Resource.Id });
        }
    }
}

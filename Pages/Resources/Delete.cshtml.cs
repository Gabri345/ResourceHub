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
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DeleteModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public Resource Resource { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resource = await _context.Resources.FirstOrDefaultAsync(m => m.Id == id);
            if (resource is null)
            {
                return NotFound();
            }

            if (!IsCurrentUserOwner(resource))
            {
                return Forbid();
            }

            Resource = resource;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resource = await _context.Resources.FindAsync(id);
            if (resource is null)
            {
                return NotFound();
            }

            if (!IsCurrentUserOwner(resource))
            {
                return Forbid();
            }

            DeleteUploadedFile(resource);
            _context.Resources.Remove(resource);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private bool IsCurrentUserOwner(Resource resource)
        {
            return resource.UploaderId == User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private void DeleteUploadedFile(Resource resource)
        {
            if (string.IsNullOrWhiteSpace(resource.FilePath))
            {
                return;
            }

            var fileName = Path.GetFileName(resource.FilePath);
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            var fullPath = Path.Combine(uploadsFolder, fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace ResourceHub.Models
{
    public class ResourceReport
    {
        public int Id { get; set; }

        public int ResourceId { get; set; }

        public Resource Resource { get; set; } = default!;

        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        public bool IsResolved { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}

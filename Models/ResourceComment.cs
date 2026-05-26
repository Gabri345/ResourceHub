using System.ComponentModel.DataAnnotations;

namespace ResourceHub.Models
{
    public class ResourceComment
    {
        public int Id { get; set; }

        public int ResourceId { get; set; }

        public Resource Resource { get; set; } = default!;

        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(700)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}

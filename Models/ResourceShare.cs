using System.ComponentModel.DataAnnotations;

namespace ResourceHub.Models
{
    public class ResourceShare
    {
        public int Id { get; set; }

        [Required]
        public int ResourceId { get; set; }

        [Required]
        [StringLength(450)]
        public string SharedWithUserId { get; set; } = string.Empty;

        public Resource? Resource { get; set; }
    }
}

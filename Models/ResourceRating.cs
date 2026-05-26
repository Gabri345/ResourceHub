using System.ComponentModel.DataAnnotations;

namespace ResourceHub.Models
{
    public class ResourceRating
    {
        public int Id { get; set; }

        public int ResourceId { get; set; }

        public Resource Resource { get; set; } = default!;

        public string UserId { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Value { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}

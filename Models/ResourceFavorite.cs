namespace ResourceHub.Models
{
    public class ResourceFavorite
    {
        public int Id { get; set; }

        public int ResourceId { get; set; }

        public Resource Resource { get; set; } = default!;

        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}

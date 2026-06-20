using System.ComponentModel.DataAnnotations;

namespace ResourceHub.Models
{
    public class Resource
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "File name")]
        public string FileName { get; set; } = string.Empty;

        [Display(Name = "File path")]
        public string FilePath { get; set; } = string.Empty;

        [Display(Name = "Uploaded on")]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(60)]
        public string Category { get; set; } = string.Empty;

        public string UploaderId { get; set; } = string.Empty;

        public ICollection<ResourceComment> Comments { get; set; } = new List<ResourceComment>();

        public ICollection<ResourceRating> Ratings { get; set; } = new List<ResourceRating>();

        public ICollection<ResourceReport> Reports { get; set; } = new List<ResourceReport>();

        public ICollection<ResourceFavorite> Favorites { get; set; } = new List<ResourceFavorite>();
    }
}

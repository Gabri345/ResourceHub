using System;
using System.ComponentModel.DataAnnotations;

namespace ResourceHub.Models
{
    public class Resource
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public string FileName { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.Now;

        public string Category { get; set; }

        public string UploaderId { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace NpsProject.Models
{
    public class NewsArticle
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "العنوان مطلوب")]
        [StringLength(200)]
        [Display(Name = "العنوان")]
        public string Title { get; set; }

        [Required(ErrorMessage = "المحتوى مطلوب")]
        [Display(Name = "المحتوى")]
        public string Content { get; set; }

        [Display(Name = "صورة الخبر")]
        public string? ImageUrl { get; set; }

        [Display(Name = "تاريخ النشر")]
        public DateTime PublishedDate { get; set; } = DateTime.Now;

        [Display(Name = "الكاتب")]
        public string? Author { get; set; }

        [Display(Name = "منشور")]
        public bool IsPublished { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}

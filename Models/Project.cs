using System.ComponentModel.DataAnnotations;

namespace NpsProject.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "العنوان مطلوب")]
        [StringLength(200)]
        [Display(Name = "العنوان")]
        public string Title { get; set; }

        [Required(ErrorMessage = "الوصف مطلوب")]
        [StringLength(1000)]
        [Display(Name = "الوصف")]
        public string Description { get; set; }

        [Display(Name = "صورة المشروع")]
        public string? ImageUrl { get; set; }

        [Display(Name = "التاجات الوصفية")]
        public string? Tags { get; set; } 

        [Display(Name = "تاريخ الإنجاز")]
        public DateTime? CompletionDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "معروض")]
        public bool IsActive { get; set; } = true;

        // Property helper للتعامل مع Tags كـ List
        public List<string> GetTagsList()
        {
            if (string.IsNullOrWhiteSpace(Tags))
                return new List<string>();

            return Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(t => t.Trim())
                       .ToList();
        }
    }
}

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace NpsProject.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "آخر تسجيل دخول")]
        public DateTime? LastLoginDate { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        [Display(Name = "الصورة الشخصية")]
        public string? ProfileImageUrl { get; set; }

        [StringLength(500)]
        [Display(Name = "نبذة")]
        public string? Bio { get; set; }
    }
}

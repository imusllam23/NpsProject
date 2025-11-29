using NpsProject.Models;

namespace NpsProject.Areas.Admin.Models
{
    public class DashboardStats
    {
        // الأخبار
        public int TotalNews { get; set; }
        public int PublishedNews { get; set; }
        public int DraftNews { get; set; }
        public int NewsThisMonth { get; set; }

        // المشاريع
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int ProjectsThisMonth { get; set; }

        // الرسائل
        public int TotalMessages { get; set; }
        public int UnreadMessages { get; set; }
        public int MessagesToday { get; set; }
        public int MessagesThisWeek { get; set; }

        // البيانات الحديثة
        public List<NewsArticle> LatestNews { get; set; }
        public List<ContactMessage> LatestMessages { get; set; }

        // معلومات المستخدم
        public string CurrentUserName { get; set; }
        public string CurrentUserRole { get; set; }
    }
}

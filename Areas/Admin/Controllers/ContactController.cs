using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpsProject.Data;
using NpsProject.Models;

namespace NpsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactController> _logger;
        private const int PageSize = 10; // عدد الرسائل في كل صفحة

        public ContactController(ApplicationDbContext context, ILogger<ContactController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Contact
        public async Task<IActionResult> Index(int page = 1, string filter = "all")
        {
            // Validation
            if (page < 1) page = 1;

            // Query أساسي
            var query = _context.ContactMessages.AsQueryable();

            // تطبيق الفلتر
            switch (filter.ToLower())
            {
                case "unread":
                    query = query.Where(m => !m.IsRead);
                    break;
                case "read":
                    query = query.Where(m => m.IsRead);
                    break;
                case "today":
                    query = query.Where(m => m.CreatedAt.Date == DateTime.Today);
                    break;
                case "week":
                    var weekAgo = DateTime.Today.AddDays(-7);
                    query = query.Where(m => m.CreatedAt >= weekAgo);
                    break;
                    // "all" - لا فلتر
            }

            // الترتيب (الأحدث أولاً)
            query = query.OrderByDescending(m => m.CreatedAt);

            // إجمالي العدد (قبل Pagination)
            var totalMessages = await query.CountAsync();

            // تطبيق Pagination
            var messages = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // حساب عدد الصفحات
            var totalPages = (int)Math.Ceiling(totalMessages / (double)PageSize);

            // إحصائيات للفلترة
            ViewBag.TotalMessages = await _context.ContactMessages.CountAsync();
            ViewBag.UnreadCount = await _context.ContactMessages.CountAsync(m => !m.IsRead);
            ViewBag.ReadCount = await _context.ContactMessages.CountAsync(m => m.IsRead);
            ViewBag.TodayCount = await _context.ContactMessages.CountAsync(m => m.CreatedAt.Date == DateTime.Today);

            // Pagination Info
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = PageSize;
            ViewBag.TotalItems = totalMessages;
            ViewBag.CurrentFilter = filter;

            return View(messages);
        }

        // GET: Admin/Contact/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("محاولة الوصول لتفاصيل رسالة بدون معرف");
                return NotFound();
            }

            var message = await _context.ContactMessages.FindAsync(id);

            if (message == null)
            {
                _logger.LogWarning($"رسالة غير موجودة: ID={id}");
                return NotFound();
            }

            // تحديد الرسالة كمقروءة تلقائياً
            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"تم تحديد الرسالة {id} كمقروءة بواسطة {User.Identity.Name}");
            }

            return View(message);
        }

        // POST: Admin/Contact/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int currentPage = 1, string currentFilter = "all")
        {
            var message = await _context.ContactMessages.FindAsync(id);

            if (message != null)
            {
                _context.ContactMessages.Remove(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"تم حذف رسالة من: {message.Email} بواسطة {User.Identity.Name}");
                TempData["SuccessMessage"] = "تم حذف الرسالة بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = "الرسالة غير موجودة";
            }

            // العودة للصفحة نفسها مع الفلتر
            return RedirectToAction(nameof(Index), new { page = currentPage, filter = currentFilter });
        }

        // POST: Admin/Contact/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);

            if (message != null)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم تحديد الرسالة كمقروءة" });
            }

            return Json(new { success = false, message = "الرسالة غير موجودة" });
        }

        // POST: Admin/Contact/MarkAsUnread/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsUnread(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);

            if (message != null)
            {
                message.IsRead = false;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم تحديد الرسالة كغير مقروءة" });
            }

            return Json(new { success = false, message = "الرسالة غير موجودة" });
        }

        // POST: Admin/Contact/DeleteMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple(int[] ids, int currentPage = 1, string currentFilter = "all")
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["ErrorMessage"] = "لم يتم تحديد أي رسائل";
                return RedirectToAction(nameof(Index), new { page = currentPage, filter = currentFilter });
            }

            try
            {
                var messages = await _context.ContactMessages
                    .Where(m => ids.Contains(m.Id))
                    .ToListAsync();

                _context.ContactMessages.RemoveRange(messages);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"تم حذف {messages.Count} رسالة بواسطة {User.Identity.Name}");
                TempData["SuccessMessage"] = $"تم حذف {messages.Count} رسالة بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف رسائل متعددة");
                TempData["ErrorMessage"] = "حدث خطأ أثناء الحذف";
            }

            return RedirectToAction(nameof(Index), new { page = currentPage, filter = currentFilter });
        }

        // GET: Admin/Contact/Export
        public async Task<IActionResult> Export()
        {
            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();

            // UTF-8 BOM للعربية في Excel
            csv.Append('\ufeff');
            csv.AppendLine("الرقم,الموضوع,البريد الإلكتروني,الرسالة,التاريخ,الوقت,مقروءة");

            foreach (var message in messages)
            {
                csv.AppendLine($"{message.Id}," +
                              $"\"{message.Subject}\"," +
                              $"{message.Email}," +
                              $"\"{message.Message.Replace("\"", "\"\"")}\"," +
                              $"{message.CreatedAt:yyyy-MM-dd}," +
                              $"{message.CreatedAt:HH:mm}," +
                              $"{(message.IsRead ? "نعم" : "لا")}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"Messages_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            _logger.LogInformation($"تم تصدير {messages.Count} رسالة بواسطة {User.Identity.Name}");

            return File(bytes, "text/csv", fileName);
        }
    }
}
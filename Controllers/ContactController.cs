using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpsProject.Data;
using NpsProject.Models;

namespace NpsProject.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactController> _logger;

        public ContactController(ApplicationDbContext context, ILogger<ContactController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========== صفحات عامة (للزوار) ==========

      

        // ========== صفحات الأدمن (محمية) ==========

        /// <summary>
        /// GET: /Contact/AdminMessages - عرض جميع رسائل التواصل
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminMessages()
        {
            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            // إحصائيات سريعة
            ViewBag.TotalMessages = messages.Count;
            ViewBag.UnreadMessages = messages.Count(m => !m.IsRead);
            ViewBag.TodayMessages = messages.Count(m => m.CreatedAt.Date == DateTime.Today);

            return View(messages);
        }

        /// <summary>
        /// GET: /Contact/Details/{id} - تفاصيل رسالة واحدة
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("محاولة الوصول لتفاصيل رسالة بدون معرف");
                return NotFound();
            }

            var message = await _context.ContactMessages
                .FirstOrDefaultAsync(m => m.Id == id);

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

        /// <summary>
        /// POST: /Contact/Delete - حذف رسالة
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);

            if (message != null)
            {
                _context.ContactMessages.Remove(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"تم حذف رسالة التواصل {id} من: {message.Email} بواسطة {User.Identity.Name}");
                TempData["SuccessMessage"] = "تم حذف الرسالة بنجاح";
            }
            else
            {
                _logger.LogWarning($"محاولة حذف رسالة غير موجودة: ID={id}");
                TempData["ErrorMessage"] = "الرسالة غير موجودة";
            }

            return RedirectToAction(nameof(AdminMessages));
        }

        /// <summary>
        /// POST: /Contact/MarkAsRead - تحديد رسالة كمقروءة
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);

            if (message != null)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"تم تحديد الرسالة {id} كمقروءة");

                return Json(new { success = true, message = "تم تحديد الرسالة كمقروءة" });
            }

            return Json(new { success = false, message = "الرسالة غير موجودة" });
        }

        /// <summary>
        /// POST: /Contact/MarkAsUnread - تحديد رسالة كغير مقروءة
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsUnread(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);

            if (message != null)
            {
                message.IsRead = false;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"تم تحديد الرسالة {id} كغير مقروءة");

                return Json(new { success = true, message = "تم تحديد الرسالة كغير مقروءة" });
            }

            return Json(new { success = false, message = "الرسالة غير موجودة" });
        }

        /// <summary>
        /// POST: /Contact/DeleteMultiple - حذف عدة رسائل
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return Json(new { success = false, message = "لم يتم تحديد أي رسائل" });
            }

            try
            {
                var messages = await _context.ContactMessages
                    .Where(m => ids.Contains(m.Id))
                    .ToListAsync();

                _context.ContactMessages.RemoveRange(messages);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"تم حذف {messages.Count} رسالة بواسطة {User.Identity.Name}");

                return Json(new { success = true, message = $"تم حذف {messages.Count} رسالة بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف رسائل متعددة");
                return Json(new { success = false, message = "حدث خطأ أثناء الحذف" });
            }
        }

        /// <summary>
        /// GET: /Contact/Export - تصدير الرسائل كـ CSV
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Export()
        {
            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("الرقم,الموضوع,البريد الإلكتروني,الرسالة,التاريخ,مقروءة");

            foreach (var message in messages)
            {
                csv.AppendLine($"{message.Id},{message.Subject},{message.Email},\"{message.Message}\",{message.CreatedAt:yyyy-MM-dd HH:mm},{(message.IsRead ? "نعم" : "لا")}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"ContactMessages_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            _logger.LogInformation($"تم تصدير {messages.Count} رسالة بواسطة {User.Identity.Name}");

            return File(bytes, "text/csv", fileName);
        }
    }
}

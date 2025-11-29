using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpsProject.Data;
using NpsProject.Helpers;
using NpsProject.Models;


namespace NpsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Editor")]
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;
        private readonly ILogger<NewsController> _logger;

        public NewsController(
            ApplicationDbContext context,
            IImageService imageService,
            ILogger<NewsController> logger)
        {
            _context = context;
            _imageService = imageService;
            _logger = logger;
        }

        // GET: Admin/News
        public async Task<IActionResult> Index()
        {
            var news = await _context.NewsArticles
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // إحصائيات
            ViewBag.TotalNews = news.Count;
            ViewBag.PublishedNews = news.Count(n => n.IsPublished);
            ViewBag.DraftNews = news.Count(n => !n.IsPublished);

            return View(news);
        }

        // GET: Admin/News/Create
        public IActionResult Create()
        {
            // القيم الافتراضية
            var article = new NewsArticle
            {
                PublishedDate = DateTime.Now,
                IsPublished = false // مسودة افتراضياً
            };

            return View(article);
        }

        // POST: Admin/News/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsArticle article, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // رفع الصورة
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        article.ImageUrl = await _imageService.SaveImageAsync(imageFile, "news");
                    }

                    // إضافة معلومات إضافية
                    article.CreatedAt = DateTime.Now;
                    if (string.IsNullOrEmpty(article.Author))
                    {
                        article.Author = User.Identity.Name;
                    }

                    _context.NewsArticles.Add(article);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"تم إضافة خبر: {article.Title} بواسطة {User.Identity.Name} - الحالة: {(article.IsPublished ? "منشور" : "مسودة")}");

                    TempData["SuccessMessage"] = article.IsPublished
                        ? "تم نشر الخبر بنجاح!"
                        : "تم حفظ الخبر كمسودة بنجاح!";

                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    _logger.LogWarning($"فشل إضافة الخبر: {ex.Message}");
                }
            }

            return View(article);
        }

        // GET: Admin/News/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("محاولة الوصول لتعديل خبر بدون معرف");
                return NotFound();
            }

            var article = await _context.NewsArticles.FindAsync(id);

            if (article == null)
            {
                _logger.LogWarning($"خبر غير موجود: ID={id}");
                return NotFound();
            }

            return View(article);
        }

        // POST: Admin/News/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NewsArticle article, IFormFile? imageFile)
        {
            if (id != article.Id)
            {
                _logger.LogWarning($"عدم تطابق ID: {id} vs {article.Id}");
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // جلب الخبر الأصلي من قاعدة البيانات
                    var existingArticle = await _context.NewsArticles.FindAsync(id);

                    if (existingArticle == null)
                    {
                        return NotFound();
                    }

                    // حفظ الصورة القديمة للمقارنة
                    var oldImageUrl = existingArticle.ImageUrl;

                    // تحديث الحقول
                    existingArticle.Title = article.Title;
                    existingArticle.Content = article.Content;
                    existingArticle.Author = article.Author;
                    existingArticle.PublishedDate = article.PublishedDate;
                    existingArticle.IsPublished = article.IsPublished;
                    existingArticle.UpdatedAt = DateTime.Now;

                    // معالجة الصورة
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // حذف الصورة القديمة
                        if (!string.IsNullOrEmpty(oldImageUrl))
                        {
                            _imageService.DeleteImage(oldImageUrl);
                        }

                        // رفع الصورة الجديدة
                        existingArticle.ImageUrl = await _imageService.SaveImageAsync(imageFile, "news");
                    }
                    else if (string.IsNullOrEmpty(article.ImageUrl) && !string.IsNullOrEmpty(oldImageUrl))
                    {
                        // إذا تم حذف الصورة من النموذج
                        _imageService.DeleteImage(oldImageUrl);
                        existingArticle.ImageUrl = null;
                    }
                    // إذا لم يتم رفع صورة جديدة، نبقي على الصورة القديمة

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"تم تحديث خبر: {existingArticle.Title} بواسطة {User.Identity.Name} - الحالة: {(existingArticle.IsPublished ? "منشور" : "مسودة")}");

                    TempData["SuccessMessage"] = existingArticle.IsPublished
                        ? "تم تحديث الخبر ونشره بنجاح!"
                        : "تم تحديث الخبر وحفظه كمسودة بنجاح!";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!NewsArticleExists(article.Id))
                    {
                        _logger.LogWarning($"خبر محذوف: ID={article.Id}");
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "خطأ في التزامن أثناء تحديث الخبر");
                        throw;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    _logger.LogWarning($"فشل تحديث الخبر: {ex.Message}");
                }
            }

            return View(article);
        }

        // POST: Admin/News/Delete/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var article = await _context.NewsArticles.FindAsync(id);

            if (article != null)
            {
                // حذف الصورة
                if (!string.IsNullOrEmpty(article.ImageUrl))
                {
                    _imageService.DeleteImage(article.ImageUrl);
                }

                _context.NewsArticles.Remove(article);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"تم حذف خبر: {article.Title} بواسطة {User.Identity.Name}");
                TempData["SuccessMessage"] = "تم حذف الخبر بنجاح";
            }
            else
            {
                _logger.LogWarning($"محاولة حذف خبر غير موجود: ID={id}");
                TempData["ErrorMessage"] = "الخبر غير موجود";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/News/TogglePublish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var article = await _context.NewsArticles.FindAsync(id);

            if (article != null)
            {
                article.IsPublished = !article.IsPublished;
                article.UpdatedAt = DateTime.Now;

                if (article.IsPublished && article.PublishedDate < DateTime.Now.AddDays(-1))
                {
                    // تحديث تاريخ النشر إذا كان قديماً
                    article.PublishedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"تم {(article.IsPublished ? "نشر" : "إلغاء نشر")} خبر: {article.Title} بواسطة {User.Identity.Name}");

                return Json(new
                {
                    success = true,
                    isPublished = article.IsPublished,
                    message = article.IsPublished ? "تم نشر الخبر" : "تم تحويل الخبر إلى مسودة"
                });
            }

            return Json(new { success = false, message = "الخبر غير موجود" });
        }

        private bool NewsArticleExists(int id)
        {
            return _context.NewsArticles.Any(e => e.Id == id);
        }
    }
}
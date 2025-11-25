using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpsProject.Data;
using NpsProject.Helpers;
using NpsProject.Models;

namespace NpsProject.Areas.admin.Controllers
{
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
        // للأدمن - عرض جميع الأخبار
        public async Task<IActionResult> Index()
        {
            var news = await _context.NewsArticles
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(news);
        }

        // للأدمن - إنشاء خبر جديد
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsArticle article, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // رفع الصورة باستخدام ImageService
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        article.ImageUrl = await _imageService.SaveImageAsync(imageFile, "news");
                    }

                    _context.NewsArticles.Add(article);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"تم إضافة الخبر: {article.Title}");
                    TempData["SuccessMessage"] = "تم إضافة الخبر بنجاح!";
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

        // للأدمن - تعديل خبر
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var article = await _context.NewsArticles.FindAsync(id);
            if (article == null)
                return NotFound();

            return View(article);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NewsArticle article, IFormFile? imageFile)
        {
            if (id != article.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // رفع صورة جديدة إذا تم اختيارها
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // حذف الصورة القديمة
                        if (!string.IsNullOrEmpty(article.ImageUrl))
                        {
                            _imageService.DeleteImage(article.ImageUrl);
                        }

                        article.ImageUrl = await _imageService.SaveImageAsync(imageFile, "news");
                    }

                    article.UpdatedAt = DateTime.Now;
                    _context.Update(article);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"تم تحديث الخبر: {article.Title}");
                    TempData["SuccessMessage"] = "تم تحديث الخبر بنجاح!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NewsArticleExists(article.Id))
                        return NotFound();
                    else
                        throw;
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    _logger.LogWarning($"فشل تحديث الخبر: {ex.Message}");
                }
            }

            return View(article);
        }

        // للأدمن - حذف خبر
        [HttpPost]
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

                _logger.LogInformation($"تم حذف الخبر: {article.Title}");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool NewsArticleExists(int id)
        {
            return _context.NewsArticles.Any(e => e.Id == id);
        }
    
}
}

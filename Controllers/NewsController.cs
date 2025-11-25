using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpsProject.Data;
using NpsProject.Helpers;
using NpsProject.Models;

namespace NpsProject.Controllers
{
    [Area("admin")]
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

        // عرض جميع الأخبار (للزوار)
        public async Task<IActionResult> Index()
        {
            var news = await _context.NewsArticles
                .Where(n => n.IsPublished)
                .OrderByDescending(n => n.PublishedDate)
                .ToListAsync();

            return View(news);
        }

        // تفاصيل خبر واحد
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var article = await _context.NewsArticles
                .FirstOrDefaultAsync(m => m.Id == id && m.IsPublished);

            if (article == null)
                return NotFound();

            return View(article);
        }

    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpsProject.Data;
using NpsProject.Helpers;
using NpsProject.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace NpsProject.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;
        private readonly ILogger<NewsController> _logger;
        private const int PageSize = 4; // عدد الرسائل في كل صفحة


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
        public async Task<IActionResult> Index(int page = 1)
        {
            var query = _context.NewsArticles.AsQueryable();

            // الترتيب (الأحدث أولاً)
            query = query.OrderByDescending(m => m.PublishedDate)
             .Where(x => x.IsPublished && x.PublishedDate.Date <= DateTime.Now);
            // آخر خبر
            var lastNews = await query.FirstOrDefaultAsync();
            // إجمالي العدد (قبل Pagination)
            var totalNews = await query.CountAsync();
           
            // تطبيق Pagination
            var news = await query
                .Skip(((page - 1) * PageSize) + 1)
                .Take(PageSize)
                .ToListAsync();

           
            // حساب عدد الصفحات
            var totalPages = (int)Math.Ceiling((totalNews - 1) / (double)PageSize);

            // Pagination Info
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = PageSize;
            ViewBag.TotalItems = totalNews - 1 ;
            ViewBag.lastNews = lastNews;


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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpsProject.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace NpsProject.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 10; 
        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;

        }
        public async Task<ActionResult> Index(int page = 1)
        {

            var totalProject = await _context.Projects.CountAsync();

            var projects = await _context.Projects
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CompletionDate)
                    .Take(PageSize)
                    .ToListAsync();

            // حساب عدد الصفحات
            var totalPages = (int)Math.Ceiling((totalProject - 1) / (double)PageSize);

            // Pagination Info
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = PageSize;
            ViewBag.TotalItems = totalProject;
            return View(projects);
            }
    }
}

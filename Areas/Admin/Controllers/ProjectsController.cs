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
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;
        private readonly ILogger<ProjectsController> _logger;
        private const int PageSize = 12; // عدد المشاريع في كل صفحة

        public ProjectsController(
            ApplicationDbContext context,
            IImageService imageService,
            ILogger<ProjectsController> logger)
        {
            _context = context;
            _imageService = imageService;
            _logger = logger;
        }

        // GET: Admin/Projects
        public async Task<IActionResult> Index(int page = 1, string filter = "all")
        {
            if (page < 1) page = 1;

            var query = _context.Projects.AsQueryable();

            // تطبيق الفلتر
            switch (filter.ToLower())
            {
                case "active":
                    query = query.Where(p => p.IsActive);
                    break;
                case "inactive":
                    query = query.Where(p => !p.IsActive);
                    break;
                case "recent":
                    var lastMonth = DateTime.Now.AddMonths(-1);
                    query = query.Where(p => p.CreatedAt >= lastMonth);
                    break;
            }

            // الترتيب
            query = query.OrderByDescending(p => p.CreatedAt);

            // إحصائيات
            ViewBag.TotalProjects = await _context.Projects.CountAsync();
            ViewBag.ActiveCount = await _context.Projects.CountAsync(p => p.IsActive);
            ViewBag.InactiveCount = await _context.Projects.CountAsync(p => !p.IsActive);
            ViewBag.RecentCount = await _context.Projects
                .CountAsync(p => p.CreatedAt >= DateTime.Now.AddMonths(-1));

            // Pagination
            var totalProjects = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalProjects / (double)PageSize);

            var projects = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalProjects;
            ViewBag.CurrentFilter = filter;

            return View(projects);
        }

        // GET: Admin/Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var project = await _context.Projects.FindAsync(id);

            if (project == null)
                return NotFound();

            return View(project);
        }

        // GET: Admin/Projects/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // رفع الصورة
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        project.ImageUrl = await _imageService.SaveImageAsync(imageFile, "projects");
                    }

                    _context.Projects.Add(project);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"تم إضافة المشروع: {project.Title} بواسطة {User.Identity.Name}");
                    TempData["SuccessMessage"] = "تم إضافة المشروع بنجاح!";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    _logger.LogWarning($"فشل إضافة المشروع: {ex.Message}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ أثناء إضافة المشروع");
                    _logger.LogError(ex, "خطأ في إضافة المشروع");
                }
            }

            return View(project);
        }

        // GET: Admin/Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            return View(project);
        }

        // POST: Admin/Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project, IFormFile? imageFile)
        {
            if (id != project.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // رفع صورة جديدة إذا تم اختيارها
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // حذف الصورة القديمة
                        if (!string.IsNullOrEmpty(project.ImageUrl))
                        {
                            _imageService.DeleteImage(project.ImageUrl);
                        }

                        project.ImageUrl = await _imageService.SaveImageAsync(imageFile, "projects");
                    }

                    _context.Update(project);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"تم تحديث المشروع: {project.Title} بواسطة {User.Identity.Name}");
                    TempData["SuccessMessage"] = "تم تحديث المشروع بنجاح!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                        return NotFound();
                    else
                        throw;
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    _logger.LogWarning($"فشل تحديث المشروع: {ex.Message}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ أثناء تحديث المشروع");
                    _logger.LogError(ex, "خطأ في تحديث المشروع");
                }
            }

            return View(project);
        }

        // POST: Admin/Projects/Delete/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int currentPage = 1, string currentFilter = "all")
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project != null)
                {
                    // حذف الصورة
                    if (!string.IsNullOrEmpty(project.ImageUrl))
                    {
                        _imageService.DeleteImage(project.ImageUrl);
                    }

                    _context.Projects.Remove(project);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"تم حذف المشروع: {project.Title} بواسطة {User.Identity.Name}");
                    TempData["SuccessMessage"] = "تم حذف المشروع بنجاح!";
                }
                else
                {
                    TempData["ErrorMessage"] = "المشروع غير موجود";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في حذف المشروع {id}");
                TempData["ErrorMessage"] = "حدث خطأ أثناء حذف المشروع";
            }

            return RedirectToAction(nameof(Index), new { page = currentPage, filter = currentFilter });
        }

        // POST: Admin/Projects/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project != null)
                {
                    project.IsActive = !project.IsActive;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"تم تغيير حالة المشروع {project.Title} إلى {(project.IsActive ? "نشط" : "غير نشط")}");

                    return Json(new
                    {
                        success = true,
                        isActive = project.IsActive,
                        message = project.IsActive ? "تم تفعيل المشروع" : "تم إيقاف المشروع"
                    });
                }

                return Json(new { success = false, message = "المشروع غير موجود" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في تغيير حالة المشروع {id}");
                return Json(new { success = false, message = "حدث خطأ" });
            }
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }
    }
}

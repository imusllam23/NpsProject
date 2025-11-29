using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpsProject.Areas.Admin.Models;
using NpsProject.Data;
using NpsProject.Models;
using NpsProject.Models.ViewModels;

namespace NpsProject.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;


        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        // GET: Admin/Account/Login
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                // user already logged in → redirect
                return RedirectToAction("Index");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;



            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة");
                    return View(model);
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "حسابك معطل. يرجى التواصل مع الإدارة");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    // تحديث آخر تسجيل دخول
                    user.LastLoginDate = DateTime.Now;
                    await _userManager.UpdateAsync(user);

                    _logger.LogInformation($"تسجيل دخول ناجح: {user.Email}");
                    return RedirectToLocal(returnUrl ?? "Index");
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning($"حساب مقفل: {model.Email}");
                    return View("Lockout");
                }

                ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة");
            }

            return View(model);
        }



        // GET: Admin/Account/
        [AllowAnonymous]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            // ---- 1) استعلام الأخبار (Query واحد) ----
            var newsStats = await _context.NewsArticles
                .GroupBy(n => 1) // GroupBy ثابت للحصول على تجميع واحد
                .Select(g => new
                {
                    Total = g.Count(),
                    Published = g.Count(n => n.IsPublished),
                    Draft = g.Count(n => !n.IsPublished),
                    ThisMonth = g.Count(n =>
                        n.CreatedAt.Month == DateTime.Now.Month &&
                        n.CreatedAt.Year == DateTime.Now.Year)
                })
                .FirstOrDefaultAsync();

            var latestNews = await _context.NewsArticles
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();


            // ---- 2) استعلام المشاريع (Query واحد) ----
            var projectStats = await _context.Projects
                .GroupBy(p => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Active = g.Count(p => p.IsActive),
                    ThisMonth = g.Count(p =>
                        p.CreatedAt.Month == DateTime.Now.Month &&
                        p.CreatedAt.Year == DateTime.Now.Year)
                })
                .FirstOrDefaultAsync();

            var latestProjects = await _context.Projects
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();


            // ---- 3) استعلام الرسائل (Query واحد) ----
            var messageStats = await _context.ContactMessages
                .GroupBy(m => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Unread = g.Count(m => !m.IsRead),
                    Today = g.Count(m => m.CreatedAt.Date == DateTime.Today),
                    ThisWeek = g.Count(m => m.CreatedAt >= DateTime.Today.AddDays(-7))
                })
                .FirstOrDefaultAsync();

            var latestMessages = await _context.ContactMessages
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToListAsync();


            // ---- تعبئة الـ ViewModel ----
            var stats = new DashboardStats
            {
                // الأخبار
                TotalNews = newsStats?.Total ?? 0,
                PublishedNews = newsStats?.Published ?? 0,
                DraftNews = newsStats?.Draft ?? 0,
                NewsThisMonth = newsStats?.ThisMonth ?? 0,
                LatestNews = latestNews,

                // المشاريع
                TotalProjects = projectStats?.Total ?? 0,
                ActiveProjects = projectStats?.Active ?? 0,
                ProjectsThisMonth = projectStats?.ThisMonth ?? 0,

                // الرسائل
                TotalMessages = messageStats?.Total ?? 0,
                UnreadMessages = messageStats?.Unread ?? 0,
                MessagesToday = messageStats?.Today ?? 0,
                MessagesThisWeek = messageStats?.ThisWeek ?? 0,
                LatestMessages = latestMessages,

                // المستخدم
                CurrentUserName = User.Identity.Name,
                CurrentUserRole = User.IsInRole("Admin") ? "مدير النظام" : "محرر"
            };

            return View(stats);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("تم تسجيل الخروج");
            return RedirectToAction("Login");
        }

        // GET: /Account/ChangePassword
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }
        // GET: Admin/Account/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Helper Methods
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction("Index");
        }

        private string TranslateIdentityError(string code)
        {
            return code switch
            {
                "DuplicateUserName" => "هذا البريد الإلكتروني مستخدم بالفعل",
                "DuplicateEmail" => "هذا البريد الإلكتروني مستخدم بالفعل",
                "InvalidEmail" => "البريد الإلكتروني غير صحيح",
                "InvalidUserName" => "اسم المستخدم غير صحيح",
                "PasswordTooShort" => "كلمة المرور قصيرة جداً",
                "PasswordRequiresNonAlphanumeric" => "كلمة المرور يجب أن تحتوي على رمز خاص",
                "PasswordRequiresDigit" => "كلمة المرور يجب أن تحتوي على رقم",
                "PasswordRequiresLower" => "كلمة المرور يجب أن تحتوي على حرف صغير",
                "PasswordRequiresUpper" => "كلمة المرور يجب أن تحتوي على حرف كبير",
                "PasswordMismatch" => "كلمة المرور الحالية غير صحيحة",
                _ => "حدث خطأ. يرجى المحاولة مرة أخرى"
            };
        }
    }
}

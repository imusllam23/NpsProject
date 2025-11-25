using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NpsProject.Models;
using NpsProject.Models.ViewModels;

namespace NpsProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: /Account/Login
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                // user already logged in → redirect
                return RedirectToAction("Index", "Home");
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
                    return RedirectToLocal(returnUrl);
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

      

      /*  // GET: /Account/Profile
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            return View(user);
        }*/

        // GET: /Account/AccessDenied
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
                return RedirectToAction("Index", "Home");
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

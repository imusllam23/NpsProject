using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpsProject.Data;
using NpsProject.Models;

namespace NpsProject.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<ActionResult> Index()
    {
        // Ã·» ¬Œ— 6 „‘«—Ì⁄
        var projects = await _context.Projects
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .ToListAsync();

        return View(projects);
    }

    
    public IActionResult Privacy()
    {
        return View();
    }


        [AllowAnonymous]
        public IActionResult contact()
        {

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] ContactMessage message)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // ≈÷«›… «·—”«·… ·ﬁ«⁄œ… «·»Ì«‰« 
                    _context.ContactMessages.Add(message);
                    await _context.SaveChangesAsync();

                    // Logging
                    _logger.LogInformation($" „ «” ·«„ —”«·… ÃœÌœ… „‰: {message.Email} - «·„Ê÷Ê⁄: {message.Subject}");

                    // —”«·… ‰Ã«Õ
                    TempData["SuccessMessage"] = " „ ≈—”«· —”«· ﬂ »‰Ã«Õ! ”‰ Ê«’· „⁄ﬂ ›Ì √ﬁ—» Êﬁ  „„ﬂ‰.";

                    // ≈⁄«œ…  ÊÃÌÂ · Ã‰» ≈⁄«œ… «·≈—”«· ⁄‰œ Refresh
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Œÿ√ ›Ì Õ›Ÿ —”«·… «· Ê«’·");
                    TempData["ErrorMessage"] = "ÕœÀ Œÿ√ √À‰«¡ ≈—”«· —”«· ﬂ. Ì—ÃÏ «·„Õ«Ê·… „—… √Œ—Ï.";
                }
            }

            // ≈–« ﬂ«‰ «·‰„Ê–Ã €Ì— ’«·Õ° ≈⁄«œ… ⁄—÷Â „⁄ «·√Œÿ«¡
            return View("contact", message);
        }
  
}

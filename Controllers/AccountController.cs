using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Models;
using SchoolManagement.ViewModels;

namespace SchoolManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _http;

        private const string SessionUserId   = "UserId";
        private const string SessionUserRole = "UserRole";
        private const string SessionUsername = "Username";

        public AccountController(AppDbContext db, IConfiguration config, IHttpContextAccessor http)
        {
            _db     = db;
            _config = config;
            _http   = http;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private int MaxAttempts    => _config.GetValue<int>("Security:MaxFailedLoginAttempts", 5);
        private int LockoutMinutes => _config.GetValue<int>("Security:LockoutMinutes", 15);

        private string? GetIp() =>
            _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        private void LogActivity(int? userId, string? username, string action, bool success, string? details = null)
        {
            _db.ActivityLogs.Add(new ActivityLog
            {
                UserId    = userId,
                Username  = username,
                Action    = action,
                IpAddress = GetIp(),
                Success   = success,
                Details   = details,
                OccurredAt = DateTime.UtcNow
            });
            _db.SaveChanges();
        }

        // ── Login ─────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32(SessionUserId) != null)
                return RedirectToDashboard();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null)
            {
                LogActivity(null, model.Username, "LoginFailed", false, "User not found");
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            // Check lockout
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                var remaining = (int)Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
                LogActivity(user.Id, user.Username, "LoginBlocked", false, $"Account locked for {remaining} more min");
                ModelState.AddModelError("", $"Account is locked. Try again in {remaining} minute(s).");
                return View(model);
            }

            if (!user.IsActive)
            {
                LogActivity(user.Id, user.Username, "LoginFailed", false, "Account inactive");
                ModelState.AddModelError("", "Your account has been deactivated. Contact an administrator.");
                return View(model);
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxAttempts)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    await _db.SaveChangesAsync();
                    LogActivity(user.Id, user.Username, "AccountLocked", false,
                        $"Locked after {MaxAttempts} failed attempts");
                    ModelState.AddModelError("", $"Too many failed attempts. Account locked for {LockoutMinutes} minutes.");
                    return View(model);
                }

                await _db.SaveChangesAsync();
                LogActivity(user.Id, user.Username, "LoginFailed", false,
                    $"Wrong password (attempt {user.FailedLoginAttempts}/{MaxAttempts})");
                ModelState.AddModelError("", $"Invalid username or password. ({user.FailedLoginAttempts}/{MaxAttempts} attempts)");
                return View(model);
            }

            // Success – reset counters
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _db.SaveChangesAsync();

            HttpContext.Session.SetInt32(SessionUserId, user.Id);
            HttpContext.Session.SetString(SessionUserRole, user.Role.ToString());
            HttpContext.Session.SetString(SessionUsername, user.Username);

            LogActivity(user.Id, user.Username, "LoginSuccess", true);
            return RedirectToDashboard();
        }

        // ── Logout ────────────────────────────────────────────────────────────

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            var userId   = HttpContext.Session.GetInt32(SessionUserId);
            var username = HttpContext.Session.GetString(SessionUsername);
            LogActivity(userId, username, "Logout", true);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ── Dashboard redirect ────────────────────────────────────────────────

        private IActionResult RedirectToDashboard()
        {
            var role = HttpContext.Session.GetString(SessionUserRole);
            return role switch
            {
                "Admin"   => RedirectToAction("Index", "Admin"),
                "Teacher" => RedirectToAction("Index", "Teacher"),
                _         => RedirectToAction("Index", "Student")
            };
        }

        // ── Change Password ───────────────────────────────────────────────────

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetInt32(SessionUserId) == null)
                return RedirectToAction("Login");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32(SessionUserId);
            if (userId == null) return RedirectToAction("Login");

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New passwords do not match.");
                return View();
            }
            if (newPassword.Length < 8)
            {
                ModelState.AddModelError("", "Password must be at least 8 characters.");
                return View();
            }

            var user = await _db.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login");

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return View();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 11);
            await _db.SaveChangesAsync();

            LogActivity(user.Id, user.Username, "PasswordChanged", true);
            TempData["Success"] = "Password changed successfully.";
            return RedirectToDashboard();
        }
    }
}

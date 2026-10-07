using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Models.DBNew2026;
using SchoolManagement.Services;
using SchoolManagement.ViewModels;

namespace SchoolManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly DBNew2026Context _db;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _http;

        private const string SessionUserId   = "UserId";
        private const string SessionUserRole = "UserRole";
        private const string SessionUsername = "Username";
        private const string SessionFullName = "FullName";

        public AccountController(DBNew2026Context db, IConfiguration config, IHttpContextAccessor http)
        {
            _db     = db;
            _config = config;
            _http   = http;
        }

        private int MaxAttempts    => _config.GetValue<int>("Security:MaxFailedLoginAttempts", 5);
        private int LockoutMinutes => _config.GetValue<int>("Security:LockoutMinutes", 15);

        private string? GetIp() =>
            _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "::1";

        private async Task LogAttemptAsync(int? userId, string email, bool success, string reason)
        {
            _db.NhatKyDangNhaps.Add(new NhatKyDangNhap
            {
                TaiKhoanId = userId,
                Email      = email,
                ThanhCong  = success,
                LyDo       = reason,
                DiaChiIp   = GetIp(),
                ThoiDiem   = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
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

            var emailInput = model.Email.Trim();
            var user = await _db.TaiKhoans
                .FirstOrDefaultAsync(u => u.Email == emailInput);

            if (user == null)
            {
                await LogAttemptAsync(null, emailInput, false, "KhongTonTai");
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Check permanent lock
            if (user.BiKhoa)
            {
                await LogAttemptAsync(user.TaiKhoanId, emailInput, false, "BiKhoaVinhVien");
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                return View(model);
            }

            // Check temporary lockout
            if (user.KhoaDen.HasValue && user.KhoaDen > DateTime.UtcNow)
            {
                var remaining = (int)Math.Ceiling((user.KhoaDen.Value - DateTime.UtcNow).TotalMinutes);
                await LogAttemptAsync(user.TaiKhoanId, emailInput, false, "KhoaTamThoi");
                ModelState.AddModelError("", $"Tài khoản đang bị tạm khóa. Vui lòng thử lại sau {remaining} phút.");
                return View(model);
            }

            // Verify password using PBKDF2/BCrypt
            if (!PasswordHasher.Verify(model.Password, user.MatKhauHash))
            {
                user.SoLanSai++;
                if (user.SoLanSai >= MaxAttempts)
                {
                    user.KhoaDen = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    await _db.SaveChangesAsync();
                    await LogAttemptAsync(user.TaiKhoanId, emailInput, false, "KhoaTamThoi");
                    ModelState.AddModelError("", $"Đăng nhập sai quá {MaxAttempts} lần. Tài khoản bị tạm khóa trong {LockoutMinutes} phút.");
                    return View(model);
                }

                await _db.SaveChangesAsync();
                await LogAttemptAsync(user.TaiKhoanId, emailInput, false, "SaiMatKhau");
                ModelState.AddModelError("", $"Mật khẩu không chính xác. (Sai {user.SoLanSai}/{MaxAttempts} lần)");
                return View(model);
            }

            // Success – reset counters
            user.SoLanSai = 0;
            user.KhoaDen = null;
            await _db.SaveChangesAsync();

            // Set session
            HttpContext.Session.SetInt32(SessionUserId, user.TaiKhoanId);
            HttpContext.Session.SetString(SessionUserRole, user.VaiTro);
            HttpContext.Session.SetString(SessionUsername, user.Email);
            HttpContext.Session.SetString(SessionFullName, user.HoTen);

            if (user.VaiTro == "GiangVien")
            {
                var gv = await _db.GiangViens.FirstOrDefaultAsync(g => g.TaiKhoanId == user.TaiKhoanId || g.Email == user.Email);
                if (gv != null)
                {
                    HttpContext.Session.SetInt32("GiangVienId", gv.GiangVienId);
                }
            }
            else if (user.VaiTro == "SinhVien")
            {
                var sv = await _db.SinhViens.FirstOrDefaultAsync(s => s.TaiKhoanId == user.TaiKhoanId || s.Email == user.Email);
                if (sv != null)
                {
                    HttpContext.Session.SetInt32("SinhVienId", sv.SinhVienId);
                }
            }

            await LogAttemptAsync(user.TaiKhoanId, emailInput, true, "ThanhCong");
            return RedirectToDashboard();
        }

        // ── Logout ────────────────────────────────────────────────────────────

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32(SessionUserId);
            var email  = HttpContext.Session.GetString(SessionUsername);
            if (!string.IsNullOrEmpty(email))
            {
                await LogAttemptAsync(userId, email, true, "DangXuat");
            }
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ── Dashboard redirect ────────────────────────────────────────────────

        private IActionResult RedirectToDashboard()
        {
            var role = HttpContext.Session.GetString(SessionUserRole);
            return role switch
            {
                "Admin"     => RedirectToAction("Index", "Admin"),
                "GiangVien" => RedirectToAction("Index", "Teacher"),
                "SinhVien"  => RedirectToAction("Index", "Student"),
                _           => RedirectToAction("Index", "Home")
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
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                return View();
            }
            if (newPassword.Length < 6)
            {
                ModelState.AddModelError("", "Mật khẩu mới phải có ít nhất 6 ký tự.");
                return View();
            }

            var user = await _db.TaiKhoans.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login");

            if (!PasswordHasher.Verify(currentPassword, user.MatKhauHash))
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không chính xác.");
                return View();
            }

            user.MatKhauHash = PasswordHasher.Hash(newPassword);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToDashboard();
        }
    }
}

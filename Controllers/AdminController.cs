using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Models.DBNew2026;
using SchoolManagement.Services;
using SchoolManagement.ViewModels;

namespace SchoolManagement.Controllers
{
    public class AdminController : Controller
    {
        private readonly DBNew2026Context _db;

        public AdminController(DBNew2026Context db) => _db = db;

        private IActionResult? RequireAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin")
                return RedirectToAction("Login", "Account");
            return null;
        }

        // ════════════════════════════════════════════════════════════════════
        // Dashboard
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Index()
        {
            if (RequireAdmin() is { } redirect) return redirect;

            ViewBag.TotalUsers    = await _db.TaiKhoans.CountAsync();
            ViewBag.TotalClasses  = await _db.LopHocs.CountAsync();
            ViewBag.TotalTeachers = await _db.GiangViens.CountAsync();
            ViewBag.TotalStudents = await _db.SinhViens.CountAsync();
            ViewBag.RecentLogs    = await _db.NhatKyDangNhaps
                .OrderByDescending(l => l.ThoiDiem)
                .Take(10)
                .ToListAsync();

            return View();
        }

        // ════════════════════════════════════════════════════════════════════
        // User Management
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Users()
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var users = await _db.TaiKhoans
                .OrderBy(u => u.VaiTro)
                .ThenBy(u => u.HoTen)
                .ToListAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            if (RequireAdmin() is { } redirect) return redirect;
            return View(new CreateUserViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim();
            if (await _db.TaiKhoans.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                return View(model);
            }

            var taiKhoan = new TaiKhoan
            {
                Email       = email,
                HoTen       = model.FullName.Trim(),
                MatKhauHash = PasswordHasher.Hash(model.Password),
                VaiTro      = model.Role,
                BiKhoa      = false,
                SoLanSai    = 0,
                KhoaDen     = null
            };
            _db.TaiKhoans.Add(taiKhoan);
            await _db.SaveChangesAsync();

            // Link to GiangVien or SinhVien
            if (model.Role == "GiangVien")
            {
                var gvCode = !string.IsNullOrWhiteSpace(model.Code) 
                    ? model.Code.Trim() 
                    : $"GV{taiKhoan.TaiKhoanId:D4}";
                _db.GiangViens.Add(new GiangVien
                {
                    MaGiangVien = gvCode,
                    HoTen       = taiKhoan.HoTen,
                    Email       = taiKhoan.Email,
                    BoMon       = "Công nghệ thông tin",
                    TaiKhoanId  = taiKhoan.TaiKhoanId
                });
                await _db.SaveChangesAsync();
            }
            else if (model.Role == "SinhVien")
            {
                var svCode = !string.IsNullOrWhiteSpace(model.Code) 
                    ? model.Code.Trim() 
                    : $"SV{taiKhoan.TaiKhoanId:D4}";
                _db.SinhViens.Add(new SinhVien
                {
                    MaSinhVien = svCode,
                    HoTen      = taiKhoan.HoTen,
                    Email      = taiKhoan.Email,
                    TaiKhoanId = taiKhoan.TaiKhoanId
                });
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = $"Đã tạo tài khoản cho '{model.FullName}'.";
            return RedirectToAction("Users");
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var user = await _db.TaiKhoans.FindAsync(id);
            if (user == null) return NotFound();

            var vm = new EditUserViewModel
            {
                Id       = user.TaiKhoanId,
                FullName = user.HoTen,
                Email    = user.Email,
                Role     = user.VaiTro,
                IsLocked = user.BiKhoa
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            if (!ModelState.IsValid) return View(model);

            var user = await _db.TaiKhoans.FindAsync(model.Id);
            if (user == null) return NotFound();

            // Prevent demoting the last active Admin
            if (user.VaiTro == "Admin" && model.Role != "Admin")
            {
                var adminCount = await _db.TaiKhoans.CountAsync(u => u.VaiTro == "Admin" && !u.BiKhoa);
                if (adminCount <= 1)
                {
                    ModelState.AddModelError("", "Không thể đổi vai trò của quản trị viên duy nhất.");
                    return View(model);
                }
            }

            user.HoTen  = model.FullName.Trim();
            user.Email  = model.Email.Trim();
            user.VaiTro = model.Role;
            user.BiKhoa = model.IsLocked;

            // Sync with GiangVien / SinhVien records
            var gv = await _db.GiangViens.FirstOrDefaultAsync(g => g.TaiKhoanId == user.TaiKhoanId);
            if (gv != null)
            {
                gv.HoTen = user.HoTen;
                gv.Email = user.Email;
            }
            var sv = await _db.SinhViens.FirstOrDefaultAsync(s => s.TaiKhoanId == user.TaiKhoanId);
            if (sv != null)
            {
                sv.HoTen = user.HoTen;
                sv.Email = user.Email;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật tài khoản thành công.";
            return RedirectToAction("Users");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var user = await _db.TaiKhoans.FindAsync(id);
            if (user == null) return NotFound();

            user.MatKhauHash = PasswordHasher.Hash(newPassword);
            user.SoLanSai    = 0;
            user.KhoaDen     = null;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã đặt lại mật khẩu cho '{user.HoTen}'.";
            return RedirectToAction("Users");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(int id)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var user = await _db.TaiKhoans.FindAsync(id);
            if (user == null) return NotFound();

            user.BiKhoa   = false;
            user.SoLanSai = 0;
            user.KhoaDen  = null;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã mở khóa tài khoản '{user.Email}'.";
            return RedirectToAction("Users");
        }

        // ════════════════════════════════════════════════════════════════════
        // Class Management
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Classes()
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var classes = await _db.LopHocs
                .Include(c => c.PhanCongGiangDays).ThenInclude(pc => pc.GiangVien)
                .Include(c => c.SinhVienLops)
                .OrderBy(c => c.TenLop)
                .ToListAsync();
            return View(classes);
        }

        [HttpGet]
        public IActionResult CreateClass()
        {
            if (RequireAdmin() is { } redirect) return redirect;
            return View(new CreateClassViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass(CreateClassViewModel model)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            if (!ModelState.IsValid) return View(model);

            if (await _db.LopHocs.AnyAsync(l => l.MaLop == model.MaLop.Trim()))
            {
                ModelState.AddModelError("MaLop", "Mã lớp này đã tồn tại.");
                return View(model);
            }

            var cls = new LopHoc
            {
                MaLop     = model.MaLop.Trim().ToUpper(),
                TenLop    = model.TenLop.Trim(),
                NamHoc    = model.NamHoc.Trim(),
                HocKy     = model.HocKy,
                SiSoToiDa = model.SiSoToiDa
            };
            _db.LopHocs.Add(cls);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo lớp '{cls.TenLop}'.";
            return RedirectToAction("ClassDetail", new { id = cls.LopHocId });
        }

        public async Task<IActionResult> ClassDetail(int id)
        {
            if (RequireAdmin() is { } redirect) return redirect;

            var cls = await _db.LopHocs
                .Include(c => c.PhanCongGiangDays).ThenInclude(pc => pc.GiangVien)
                .Include(c => c.SinhVienLops).ThenInclude(svl => svl.SinhVien)
                .Include(c => c.BaiHocs)
                .Include(c => c.BaiTaps)
                .FirstOrDefaultAsync(c => c.LopHocId == id);

            if (cls == null) return NotFound();

            var vm = new ClassDetailViewModel
            {
                Class       = cls,
                Teachers    = cls.PhanCongGiangDays.Select(pc => pc.GiangVien).ToList(),
                Students    = cls.SinhVienLops.Select(svl => svl.SinhVien).ToList(),
                Lessons     = cls.BaiHocs.OrderByDescending(l => l.NgayTao).ToList(),
                Assignments = cls.BaiTaps.OrderByDescending(a => a.NgayTao).ToList()
            };
            return View(vm);
        }

        // Add Teacher to Class
        [HttpGet]
        public async Task<IActionResult> AddTeacher(int classId)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var cls = await _db.LopHocs.FindAsync(classId);
            if (cls == null) return NotFound();

            var assignedTeacherIds = await _db.PhanCongGiangDays
                .Where(pc => pc.LopHocId == classId)
                .Select(pc => pc.GiangVienId)
                .ToListAsync();

            var available = await _db.GiangViens
                .Where(g => !assignedTeacherIds.Contains(g.GiangVienId))
                .Select(g => new SelectMemberItem
                {
                    Id    = g.GiangVienId,
                    Name  = g.HoTen,
                    Code  = g.MaGiangVien,
                    Email = g.Email
                })
                .ToListAsync();

            var vm = new AddMemberViewModel
            {
                ClassId        = classId,
                ClassName      = $"{cls.MaLop} - {cls.TenLop}",
                MemberType     = "Teacher",
                AvailableUsers = available,
                RoleInClass    = "Giảng viên chính"
            };
            return View("AddMember", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher(int classId, int selectedUserId, string? roleInClass)
        {
            if (RequireAdmin() is { } redirect) return redirect;

            var exists = await _db.PhanCongGiangDays
                .AnyAsync(pc => pc.LopHocId == classId && pc.GiangVienId == selectedUserId);

            if (!exists)
            {
                _db.PhanCongGiangDays.Add(new PhanCongGiangDay
                {
                    LopHocId    = classId,
                    GiangVienId = selectedUserId,
                    VaiTro      = string.IsNullOrWhiteSpace(roleInClass) ? "Giảng viên chính" : roleInClass.Trim()
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã phân công giảng viên vào lớp.";
            }
            return RedirectToAction("ClassDetail", new { id = classId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTeacher(int classId, int teacherId)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var pc = await _db.PhanCongGiangDays
                .FirstOrDefaultAsync(x => x.LopHocId == classId && x.GiangVienId == teacherId);
            if (pc != null)
            {
                _db.PhanCongGiangDays.Remove(pc);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã hủy phân công giảng viên.";
            }
            return RedirectToAction("ClassDetail", new { id = classId });
        }

        // Add Student to Class
        [HttpGet]
        public async Task<IActionResult> AddStudent(int classId)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var cls = await _db.LopHocs.FindAsync(classId);
            if (cls == null) return NotFound();

            var enrolledStudentIds = await _db.SinhVienLops
                .Where(s => s.LopHocId == classId)
                .Select(s => s.SinhVienId)
                .ToListAsync();

            var available = await _db.SinhViens
                .Where(s => !enrolledStudentIds.Contains(s.SinhVienId))
                .Select(s => new SelectMemberItem
                {
                    Id    = s.SinhVienId,
                    Name  = s.HoTen,
                    Code  = s.MaSinhVien,
                    Email = s.Email
                })
                .ToListAsync();

            var vm = new AddMemberViewModel
            {
                ClassId        = classId,
                ClassName      = $"{cls.MaLop} - {cls.TenLop}",
                MemberType     = "Student",
                AvailableUsers = available
            };
            return View("AddMember", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(int classId, int selectedUserId)
        {
            if (RequireAdmin() is { } redirect) return redirect;

            var exists = await _db.SinhVienLops
                .AnyAsync(s => s.LopHocId == classId && s.SinhVienId == selectedUserId);

            if (!exists)
            {
                _db.SinhVienLops.Add(new SinhVienLop
                {
                    LopHocId   = classId,
                    SinhVienId = selectedUserId,
                    NgayVaoLop = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã thêm sinh viên vào lớp.";
            }
            return RedirectToAction("ClassDetail", new { id = classId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int classId, int studentId)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var svl = await _db.SinhVienLops
                .FirstOrDefaultAsync(x => x.LopHocId == classId && x.SinhVienId == studentId);
            if (svl != null)
            {
                _db.SinhVienLops.Remove(svl);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa sinh viên khỏi lớp.";
            }
            return RedirectToAction("ClassDetail", new { id = classId });
        }

        // ════════════════════════════════════════════════════════════════════
        // Activity Logs
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> ActivityLogs(int page = 1)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            const int pageSize = 30;

            var logs = await _db.NhatKyDangNhaps
                .OrderByDescending(l => l.ThoiDiem)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page      = page;
            ViewBag.TotalLogs = await _db.NhatKyDangNhaps.CountAsync();
            ViewBag.PageSize  = pageSize;
            return View(logs);
        }
    }
}

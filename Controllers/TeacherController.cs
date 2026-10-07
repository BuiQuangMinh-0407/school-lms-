using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Models.DBNew2026;
using SchoolManagement.ViewModels;

namespace SchoolManagement.Controllers
{
    public class TeacherController : Controller
    {
        private readonly DBNew2026Context _db;

        public TeacherController(DBNew2026Context db) => _db = db;

        private IActionResult? RequireTeacher()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "GiangVien" && role != "Admin")
                return RedirectToAction("Login", "Account");
            return null;
        }

        private async Task<int?> GetCurrentTeacherIdAsync()
        {
            var cached = HttpContext.Session.GetInt32("GiangVienId");
            if (cached.HasValue) return cached.Value;

            var uid = HttpContext.Session.GetInt32("UserId");
            if (!uid.HasValue) return null;

            var gv = await _db.GiangViens.FirstOrDefaultAsync(g => g.TaiKhoanId == uid.Value);
            if (gv != null)
            {
                HttpContext.Session.SetInt32("GiangVienId", gv.GiangVienId);
                return gv.GiangVienId;
            }
            return null;
        }

        private async Task<bool> TeacherOwnsClassAsync(int classId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Admin") return true;

            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue) return false;

            return await _db.PhanCongGiangDays
                .AnyAsync(pc => pc.LopHocId == classId && pc.GiangVienId == teacherId.Value);
        }

        // ════════════════════════════════════════════════════════════════════
        // Dashboard
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Index()
        {
            if (RequireTeacher() is { } r) return r;

            var role = HttpContext.Session.GetString("UserRole");
            List<LopHoc> classes;

            if (role == "Admin")
            {
                classes = await _db.LopHocs
                    .Include(c => c.SinhVienLops)
                    .OrderBy(c => c.TenLop)
                    .ToListAsync();
            }
            else
            {
                var teacherId = await GetCurrentTeacherIdAsync();
                if (!teacherId.HasValue)
                {
                    TempData["Error"] = "Tài khoản chưa được liên kết với hồ sơ giảng viên.";
                    return View(new List<LopHoc>());
                }

                classes = await _db.PhanCongGiangDays
                    .Where(pc => pc.GiangVienId == teacherId.Value)
                    .Include(pc => pc.LopHoc)
                        .ThenInclude(l => l.SinhVienLops)
                    .Select(pc => pc.LopHoc)
                    .OrderBy(l => l.TenLop)
                    .ToListAsync();
            }

            return View(classes);
        }

        // ════════════════════════════════════════════════════════════════════
        // Class Detail
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> ClassDetail(int id)
        {
            if (RequireTeacher() is { } r) return r;
            if (!await TeacherOwnsClassAsync(id)) return Forbid();

            var cls = await _db.LopHocs
                .Include(c => c.SinhVienLops).ThenInclude(svl => svl.SinhVien)
                .Include(c => c.BaiHocs)
                .Include(c => c.BaiTaps)
                .FirstOrDefaultAsync(c => c.LopHocId == id);

            if (cls == null) return NotFound();

            var vm = new ClassDetailViewModel
            {
                Class       = cls,
                Students    = cls.SinhVienLops.Select(svl => svl.SinhVien).ToList(),
                Lessons     = cls.BaiHocs.OrderByDescending(l => l.NgayTao).ToList(),
                Assignments = cls.BaiTaps.OrderByDescending(a => a.NgayTao).ToList()
            };
            return View(vm);
        }

        // ════════════════════════════════════════════════════════════════════
        // Lessons (BaiHoc)
        // ════════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> CreateLesson(int classId)
        {
            if (RequireTeacher() is { } r) return r;
            if (!await TeacherOwnsClassAsync(classId)) return Forbid();

            return View(new CreateLessonViewModel { ClassId = classId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(CreateLessonViewModel model)
        {
            if (RequireTeacher() is { } r) return r;
            if (!await TeacherOwnsClassAsync(model.ClassId)) return Forbid();
            if (!ModelState.IsValid) return View(model);

            var lesson = new BaiHoc
            {
                LopHocId    = model.ClassId,
                TieuDe      = model.Title.Trim(),
                NoiDung     = model.Content.Trim(),
                DaXuatBan   = model.IsPublished,
                NgayTao     = DateTime.UtcNow,
                NgayXuatBan = model.IsPublished ? DateTime.UtcNow : null
            };
            _db.BaiHocs.Add(lesson);
            await _db.SaveChangesAsync();

            TempData["Success"] = model.IsPublished 
                ? "Đã tạo và xuất bản bài học thành công!" 
                : "Đã lưu bản nháp bài học.";
            return RedirectToAction("ClassDetail", new { id = model.ClassId });
        }

        [HttpGet]
        public async Task<IActionResult> EditLesson(int id)
        {
            if (RequireTeacher() is { } r) return r;
            var lesson = await _db.BaiHocs.FindAsync(id);
            if (lesson == null) return NotFound();
            if (!await TeacherOwnsClassAsync(lesson.LopHocId)) return Forbid();

            return View(new CreateLessonViewModel
            {
                ClassId     = lesson.LopHocId,
                LessonId    = lesson.BaiHocId,
                Title       = lesson.TieuDe,
                Content     = lesson.NoiDung,
                IsPublished = lesson.DaXuatBan
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(int id, CreateLessonViewModel model)
        {
            if (RequireTeacher() is { } r) return r;
            var lesson = await _db.BaiHocs.FindAsync(id);
            if (lesson == null) return NotFound();
            if (!await TeacherOwnsClassAsync(lesson.LopHocId)) return Forbid();
            if (!ModelState.IsValid) return View(model);

            lesson.TieuDe = model.Title.Trim();
            lesson.NoiDung = model.Content.Trim();
            if (!lesson.DaXuatBan && model.IsPublished)
            {
                lesson.DaXuatBan   = true;
                lesson.NgayXuatBan = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật bài học.";
            return RedirectToAction("ClassDetail", new { id = lesson.LopHocId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishLesson(int id)
        {
            if (RequireTeacher() is { } r) return r;
            var lesson = await _db.BaiHocs.FindAsync(id);
            if (lesson == null) return NotFound();
            if (!await TeacherOwnsClassAsync(lesson.LopHocId)) return Forbid();

            lesson.DaXuatBan   = true;
            lesson.NgayXuatBan = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã xuất bản bài học. Sinh viên có thể xem nội dung ngay bây giờ.";
            return RedirectToAction("ClassDetail", new { id = lesson.LopHocId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpublishLesson(int id)
        {
            if (RequireTeacher() is { } r) return r;
            var lesson = await _db.BaiHocs.FindAsync(id);
            if (lesson == null) return NotFound();
            if (!await TeacherOwnsClassAsync(lesson.LopHocId)) return Forbid();

            lesson.DaXuatBan   = false;
            lesson.NgayXuatBan = null;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã chuyển bài học về trạng thái nháp.";
            return RedirectToAction("ClassDetail", new { id = lesson.LopHocId });
        }

        // ════════════════════════════════════════════════════════════════════
        // Assignments (BaiTap)
        // ════════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> CreateAssignment(int classId)
        {
            if (RequireTeacher() is { } r) return r;
            if (!await TeacherOwnsClassAsync(classId)) return Forbid();

            return View(new CreateAssignmentViewModel { ClassId = classId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssignment(CreateAssignmentViewModel model)
        {
            if (RequireTeacher() is { } r) return r;
            if (!await TeacherOwnsClassAsync(model.ClassId)) return Forbid();
            if (!ModelState.IsValid) return View(model);

            var assignment = new BaiTap
            {
                LopHocId    = model.ClassId,
                TieuDe      = model.Title.Trim(),
                MoTa        = model.Description.Trim(),
                HanNop      = model.DueDate,
                DaXuatBan   = model.IsPublished,
                NgayTao     = DateTime.UtcNow,
                NgayXuatBan = model.IsPublished ? DateTime.UtcNow : null
            };
            _db.BaiTaps.Add(assignment);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã tạo bài tập mới.";
            return RedirectToAction("ClassDetail", new { id = model.ClassId });
        }

        // ════════════════════════════════════════════════════════════════════
        // Submissions & Grading (BaiNop)
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Submissions(int assignmentId)
        {
            if (RequireTeacher() is { } r) return r;

            var assignment = await _db.BaiTaps
                .Include(a => a.LopHoc)
                .Include(a => a.BaiNops)
                    .ThenInclude(b => b.SinhVien)
                .FirstOrDefaultAsync(a => a.BaiTapId == assignmentId);

            if (assignment == null) return NotFound();
            if (!await TeacherOwnsClassAsync(assignment.LopHocId)) return Forbid();

            ViewBag.Assignment = assignment;
            return View(assignment.BaiNops.OrderBy(s => s.NgayNop).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Grade(int submissionId)
        {
            if (RequireTeacher() is { } r) return r;

            var sub = await _db.BaiNops
                .Include(s => s.BaiTap)
                .Include(s => s.SinhVien)
                .FirstOrDefaultAsync(s => s.BaiNopId == submissionId);

            if (sub == null) return NotFound();
            if (!await TeacherOwnsClassAsync(sub.BaiTap.LopHocId)) return Forbid();

            var vm = new GradeSubmissionViewModel
            {
                SubmissionId      = sub.BaiNopId,
                StudentName       = sub.SinhVien.HoTen,
                StudentCode       = sub.SinhVien.MaSinhVien,
                SubmissionContent = sub.NoiDung,
                SubmittedAt       = sub.NgayNop,
                Grade             = sub.Diem ?? 0,
                Feedback          = sub.NhanXet
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(GradeSubmissionViewModel model)
        {
            if (RequireTeacher() is { } r) return r;
            if (!ModelState.IsValid) return View(model);

            var sub = await _db.BaiNops
                .Include(s => s.BaiTap)
                .FirstOrDefaultAsync(s => s.BaiNopId == model.SubmissionId);

            if (sub == null) return NotFound();
            if (!await TeacherOwnsClassAsync(sub.BaiTap.LopHocId)) return Forbid();

            sub.Diem     = model.Grade;
            sub.NhanXet  = model.Feedback?.Trim();
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã lưu điểm và nhận xét cho bài nộp.";
            return RedirectToAction("Submissions", new { assignmentId = sub.BaiTapId });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Models.DBNew2026;
using SchoolManagement.ViewModels;

namespace SchoolManagement.Controllers
{
    public class StudentController : Controller
    {
        private readonly DBNew2026Context _db;

        public StudentController(DBNew2026Context db) => _db = db;

        private IActionResult? RequireStudent()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "SinhVien")
                return RedirectToAction("Login", "Account");
            return null;
        }

        private async Task<int?> GetCurrentStudentIdAsync()
        {
            var cached = HttpContext.Session.GetInt32("SinhVienId");
            if (cached.HasValue) return cached.Value;

            var uid = HttpContext.Session.GetInt32("UserId");
            if (!uid.HasValue) return null;

            var sv = await _db.SinhViens.FirstOrDefaultAsync(s => s.TaiKhoanId == uid.Value);
            if (sv != null)
            {
                HttpContext.Session.SetInt32("SinhVienId", sv.SinhVienId);
                return sv.SinhVienId;
            }
            return null;
        }

        private async Task<bool> StudentEnrolledAsync(int classId)
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue) return false;

            return await _db.SinhVienLops
                .AnyAsync(s => s.LopHocId == classId && s.SinhVienId == studentId.Value);
        }

        // ════════════════════════════════════════════════════════════════════
        // Dashboard
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Index()
        {
            if (RequireStudent() is { } r) return r;

            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue)
            {
                TempData["Error"] = "Tài khoản chưa được liên kết với hồ sơ sinh viên.";
                return View(new List<LopHoc>());
            }

            var classes = await _db.SinhVienLops
                .Where(s => s.SinhVienId == studentId.Value)
                .Include(s => s.LopHoc)
                    .ThenInclude(l => l.BaiHocs.Where(b => b.DaXuatBan))
                .Include(s => s.LopHoc)
                    .ThenInclude(l => l.BaiTaps.Where(b => b.DaXuatBan))
                .Select(s => s.LopHoc)
                .OrderBy(l => l.TenLop)
                .ToListAsync();

            return View(classes);
        }

        // ════════════════════════════════════════════════════════════════════
        // Class Detail – strictly published content only
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> ClassDetail(int id)
        {
            if (RequireStudent() is { } r) return r;
            if (!await StudentEnrolledAsync(id)) return Forbid();

            var cls = await _db.LopHocs
                .Include(c => c.BaiHocs.Where(l => l.DaXuatBan))
                .Include(c => c.BaiTaps.Where(a => a.DaXuatBan))
                .FirstOrDefaultAsync(c => c.LopHocId == id);

            if (cls == null) return NotFound();

            var vm = new ClassDetailViewModel
            {
                Class       = cls,
                Lessons     = cls.BaiHocs.OrderByDescending(l => l.NgayXuatBan ?? l.NgayTao).ToList(),
                Assignments = cls.BaiTaps.OrderBy(a => a.HanNop).ToList()
            };
            return View(vm);
        }

        // ════════════════════════════════════════════════════════════════════
        // Lesson View
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> ViewLesson(int id)
        {
            if (RequireStudent() is { } r) return r;

            var lesson = await _db.BaiHocs
                .Include(l => l.LopHoc)
                .FirstOrDefaultAsync(l => l.BaiHocId == id && l.DaXuatBan);

            if (lesson == null) return NotFound();
            if (!await StudentEnrolledAsync(lesson.LopHocId)) return Forbid();

            return View(lesson);
        }

        // ════════════════════════════════════════════════════════════════════
        // Assignment View & Submission
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> ViewAssignment(int id)
        {
            if (RequireStudent() is { } r) return r;

            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue) return Forbid();

            var assignment = await _db.BaiTaps
                .Include(a => a.LopHoc)
                .FirstOrDefaultAsync(a => a.BaiTapId == id && a.DaXuatBan);

            if (assignment == null) return NotFound();
            if (!await StudentEnrolledAsync(assignment.LopHocId)) return Forbid();

            var existingSub = await _db.BaiNops
                .FirstOrDefaultAsync(s => s.BaiTapId == id && s.SinhVienId == studentId.Value);

            var vm = new SubmitAssignmentViewModel
            {
                AssignmentId          = assignment.BaiTapId,
                AssignmentTitle       = assignment.TieuDe,
                AssignmentDescription = assignment.MoTa,
                DueDate               = assignment.HanNop,
                SubmissionContent     = existingSub?.NoiDung ?? string.Empty
            };

            ViewBag.ExistingSubmission = existingSub;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAssignment(SubmitAssignmentViewModel model)
        {
            if (RequireStudent() is { } r) return r;

            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue) return Forbid();

            var assignment = await _db.BaiTaps.FindAsync(model.AssignmentId);
            if (assignment == null || !assignment.DaXuatBan) return NotFound();
            if (!await StudentEnrolledAsync(assignment.LopHocId)) return Forbid();

            if (!ModelState.IsValid)
            {
                var existingSub = await _db.BaiNops
                    .FirstOrDefaultAsync(s => s.BaiTapId == model.AssignmentId && s.SinhVienId == studentId.Value);
                ViewBag.ExistingSubmission = existingSub;
                return View("ViewAssignment", model);
            }

            var existing = await _db.BaiNops
                .FirstOrDefaultAsync(s => s.BaiTapId == model.AssignmentId && s.SinhVienId == studentId.Value);

            if (existing != null)
            {
                existing.NoiDung = model.SubmissionContent.Trim();
                existing.NgayNop = DateTime.UtcNow;
                existing.Diem    = null;
                existing.NhanXet = null;
            }
            else
            {
                _db.BaiNops.Add(new BaiNop
                {
                    BaiTapId   = model.AssignmentId,
                    SinhVienId = studentId.Value,
                    NoiDung    = model.SubmissionContent.Trim(),
                    NgayNop    = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Nộp bài thành công!";
            return RedirectToAction("ClassDetail", new { id = assignment.LopHocId });
        }

        // ════════════════════════════════════════════════════════════════════
        // My Grades
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> MyGrades()
        {
            if (RequireStudent() is { } r) return r;

            var studentId = await GetCurrentStudentIdAsync();
            if (!studentId.HasValue) return View(new List<BaiNop>());

            var submissions = await _db.BaiNops
                .Include(s => s.BaiTap)
                    .ThenInclude(a => a.LopHoc)
                .Where(s => s.SinhVienId == studentId.Value && s.Diem != null)
                .OrderByDescending(s => s.NgayNop)
                .ToListAsync();

            return View(submissions);
        }
    }
}

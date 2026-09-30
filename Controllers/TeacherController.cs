using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Models;
using SchoolManagement.ViewModels;

namespace SchoolManagement.Controllers
{
    public class TeacherController : Controller
    {
        private readonly AppDbContext _db;
        public TeacherController(AppDbContext db) => _db = db;

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

        private IActionResult? RequireTeacher()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Teacher" && role != "Admin")
                return RedirectToAction("Login", "Account");
            return null;
        }

        /// <summary>Returns true if the teacher is assigned to this class.</summary>
        private async Task<bool> TeacherOwnsClassAsync(int classId)
        {
            var uid = CurrentUserId;
            if (uid == null) return false;
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Admin") return true;
            return await _db.ClassTeachers
                .AnyAsync(ct => ct.ClassId == classId && ct.TeacherId == uid.Value);
        }

        // ════════════════════════════════════════════════════════════════════
        // Dashboard – list classes assigned to this teacher
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Index()
        {
            if (RequireTeacher() is { } r) return r;
            var uid = CurrentUserId!.Value;

            var classes = await _db.ClassTeachers
                .Where(ct => ct.TeacherId == uid)
                .Include(ct => ct.Class)
                    .ThenInclude(c => c.ClassStudents)
                .Select(ct => ct.Class)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(classes);
        }

        // ════════════════════════════════════════════════════════════════════
        // Class Detail
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> ClassDetail(int id)
        {
            if (RequireTeacher() is { } r) return r;
            if (!await TeacherOwnsClassAsync(id)) return Forbid();

            var cls = await _db.Classes
                .Include(c => c.ClassStudents).ThenInclude(cs => cs.Student)
                .Include(c => c.Lessons)
                .Include(c => c.Assignments)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (cls == null) return NotFound();

            var vm = new ClassDetailViewModel
            {
                Class       = cls,
                Students    = cls.ClassStudents.Select(cs => cs.Student).ToList(),
                Lessons     = cls.Lessons.OrderByDescending(l => l.CreatedAt).ToList(),
                Assignments = cls.Assignments.OrderByDescending(a => a.DueDate).ToList()
            };
            return View(vm);
        }

        // ════════════════════════════════════════════════════════════════════
        // Lessons
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

            var lesson = new Lesson
            {
                ClassId   = model.ClassId,
                TeacherId = CurrentUserId!.Value,
                Title     = model.Title,
                Content   = model.Content
            };
            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Lesson created.";
            return RedirectToAction("ClassDetail", new { id = model.ClassId });
        }

        [HttpGet]
        public async Task<IActionResult> EditLesson(int id)
        {
            if (RequireTeacher() is { } r) return r;
            var lesson = await _db.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();
            if (!await TeacherOwnsClassAsync(lesson.ClassId)) return Forbid();

            return View(new CreateLessonViewModel
            {
                ClassId = lesson.ClassId,
                Title   = lesson.Title,
                Content = lesson.Content
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(int id, CreateLessonViewModel model)
        {
            if (RequireTeacher() is { } r) return r;
            var lesson = await _db.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();
            if (!await TeacherOwnsClassAsync(lesson.ClassId)) return Forbid();
            if (!ModelState.IsValid) return View(model);

            lesson.Title     = model.Title;
            lesson.Content   = model.Content;
            lesson.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Lesson updated.";
            return RedirectToAction("ClassDetail", new { id = lesson.ClassId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishLesson(int id)
        {
            if (RequireTeacher() is { } r) return r;
            var lesson = await _db.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();
            if (!await TeacherOwnsClassAsync(lesson.ClassId)) return Forbid();

            lesson.IsPublished = true;
            lesson.PublishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Lesson published. Students can now view it.";
            return RedirectToAction("ClassDetail", new { id = lesson.ClassId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpublishLesson(int id)
        {
            if (RequireTeacher() is { } r) return r;
            var lesson = await _db.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();
            if (!await TeacherOwnsClassAsync(lesson.ClassId)) return Forbid();

            lesson.IsPublished = false;
            lesson.PublishedAt = null;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Lesson unpublished.";
            return RedirectToAction("ClassDetail", new { id = lesson.ClassId });
        }

        // ════════════════════════════════════════════════════════════════════
        // Assignments
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

            var assignment = new Assignment
            {
                ClassId     = model.ClassId,
                TeacherId   = CurrentUserId!.Value,
                Title       = model.Title,
                Description = model.Description,
                DueDate     = model.DueDate.ToUniversalTime()
            };
            _db.Assignments.Add(assignment);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Assignment created.";
            return RedirectToAction("ClassDetail", new { id = model.ClassId });
        }

        // ════════════════════════════════════════════════════════════════════
        // Grading
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Show all submissions for an assignment (only the teacher's class).</summary>
        public async Task<IActionResult> Submissions(int assignmentId)
        {
            if (RequireTeacher() is { } r) return r;
            var assignment = await _db.Assignments
                .Include(a => a.Submissions)
                    .ThenInclude(s => s.Student)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null) return NotFound();
            if (!await TeacherOwnsClassAsync(assignment.ClassId)) return Forbid();

            ViewBag.Assignment = assignment;
            return View(assignment.Submissions.OrderBy(s => s.SubmittedAt).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Grade(int submissionId)
        {
            if (RequireTeacher() is { } r) return r;
            var sub = await _db.AssignmentSubmissions
                .Include(s => s.Assignment)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == submissionId);
            if (sub == null) return NotFound();
            if (!await TeacherOwnsClassAsync(sub.Assignment.ClassId)) return Forbid();

            var vm = new GradeSubmissionViewModel
            {
                SubmissionId      = sub.Id,
                StudentName       = sub.Student.FullName,
                SubmissionContent = sub.SubmissionContent,
                SubmittedAt       = sub.SubmittedAt,
                Grade             = sub.Grade ?? 0,
                Feedback          = sub.Feedback
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(GradeSubmissionViewModel model)
        {
            if (RequireTeacher() is { } r) return r;
            if (!ModelState.IsValid) return View(model);

            var sub = await _db.AssignmentSubmissions
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.Id == model.SubmissionId);
            if (sub == null) return NotFound();
            if (!await TeacherOwnsClassAsync(sub.Assignment.ClassId)) return Forbid();

            sub.Grade             = model.Grade;
            sub.Feedback          = model.Feedback;
            sub.GradedAt          = DateTime.UtcNow;
            sub.GradedByTeacherId = CurrentUserId!.Value;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Grade saved.";
            return RedirectToAction("Submissions", new { assignmentId = sub.AssignmentId });
        }
    }
}

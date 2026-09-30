using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Models;
using SchoolManagement.ViewModels;

namespace SchoolManagement.Controllers
{
    public class StudentController : Controller
    {
        private readonly AppDbContext _db;
        public StudentController(AppDbContext db) => _db = db;

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

        private IActionResult? RequireStudent()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Student")
                return RedirectToAction("Login", "Account");
            return null;
        }

        /// <summary>Returns true if the student is enrolled in this class.</summary>
        private async Task<bool> StudentEnrolledAsync(int classId)
        {
            var uid = CurrentUserId;
            if (uid == null) return false;
            return await _db.ClassStudents
                .AnyAsync(cs => cs.ClassId == classId && cs.StudentId == uid.Value);
        }

        // ════════════════════════════════════════════════════════════════════
        // Dashboard – list enrolled classes
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Index()
        {
            if (RequireStudent() is { } r) return r;
            var uid = CurrentUserId!.Value;

            var classes = await _db.ClassStudents
                .Where(cs => cs.StudentId == uid)
                .Include(cs => cs.Class)
                    .ThenInclude(c => c.Lessons.Where(l => l.IsPublished))
                .Include(cs => cs.Class)
                    .ThenInclude(c => c.Assignments)
                .Select(cs => cs.Class)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(classes);
        }

        // ════════════════════════════════════════════════════════════════════
        // Class Detail – see published lessons & assignments
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> ClassDetail(int id)
        {
            if (RequireStudent() is { } r) return r;
            if (!await StudentEnrolledAsync(id)) return Forbid();

            var cls = await _db.Classes
                .Include(c => c.Lessons.Where(l => l.IsPublished))   // Only published!
                .Include(c => c.Assignments)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (cls == null) return NotFound();

            var vm = new ClassDetailViewModel
            {
                Class       = cls,
                Lessons     = cls.Lessons.OrderByDescending(l => l.PublishedAt).ToList(),
                Assignments = cls.Assignments.OrderBy(a => a.DueDate).ToList()
            };
            return View(vm);
        }

        // ════════════════════════════════════════════════════════════════════
        // Lesson View
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> ViewLesson(int id)
        {
            if (RequireStudent() is { } r) return r;
            var lesson = await _db.Lessons
                .Include(l => l.Class)
                .Include(l => l.Teacher)
                .FirstOrDefaultAsync(l => l.Id == id && l.IsPublished);
            if (lesson == null) return NotFound();
            if (!await StudentEnrolledAsync(lesson.ClassId)) return Forbid();
            return View(lesson);
        }

        // ════════════════════════════════════════════════════════════════════
        // Assignments & Submissions
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> ViewAssignment(int id)
        {
            if (RequireStudent() is { } r) return r;
            var uid = CurrentUserId!.Value;

            var assignment = await _db.Assignments
                .Include(a => a.Class)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (assignment == null) return NotFound();
            if (!await StudentEnrolledAsync(assignment.ClassId)) return Forbid();

            // Check if student already submitted
            var existingSub = await _db.AssignmentSubmissions
                .FirstOrDefaultAsync(s => s.AssignmentId == id && s.StudentId == uid);

            var vm = new SubmitAssignmentViewModel
            {
                AssignmentId          = assignment.Id,
                AssignmentTitle       = assignment.Title,
                AssignmentDescription = assignment.Description,
                DueDate               = assignment.DueDate,
                SubmissionContent     = existingSub?.SubmissionContent ?? string.Empty
            };

            ViewBag.ExistingSubmission = existingSub;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAssignment(SubmitAssignmentViewModel model)
        {
            if (RequireStudent() is { } r) return r;
            var uid = CurrentUserId!.Value;

            var assignment = await _db.Assignments.FindAsync(model.AssignmentId);
            if (assignment == null) return NotFound();
            if (!await StudentEnrolledAsync(assignment.ClassId)) return Forbid();
            if (!ModelState.IsValid) return View("ViewAssignment", model);

            var existing = await _db.AssignmentSubmissions
                .FirstOrDefaultAsync(s => s.AssignmentId == model.AssignmentId && s.StudentId == uid);

            if (existing != null)
            {
                // Update existing submission (re-submit)
                existing.SubmissionContent = model.SubmissionContent;
                existing.SubmittedAt       = DateTime.UtcNow;
                existing.Grade             = null;  // Reset grade on re-submit
                existing.Feedback          = null;
                existing.GradedAt          = null;
            }
            else
            {
                _db.AssignmentSubmissions.Add(new AssignmentSubmission
                {
                    AssignmentId      = model.AssignmentId,
                    StudentId         = uid,
                    SubmissionContent = model.SubmissionContent
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Assignment submitted successfully!";
            return RedirectToAction("ClassDetail", new { id = assignment.ClassId });
        }

        // ════════════════════════════════════════════════════════════════════
        // My Grades
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> MyGrades()
        {
            if (RequireStudent() is { } r) return r;
            var uid = CurrentUserId!.Value;

            var submissions = await _db.AssignmentSubmissions
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Class)
                .Where(s => s.StudentId == uid && s.Grade != null)
                .OrderByDescending(s => s.GradedAt)
                .ToListAsync();
            return View(submissions);
        }
    }
}

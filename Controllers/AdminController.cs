using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Models;
using SchoolManagement.ViewModels;

namespace SchoolManagement.Controllers
{
    /// <summary>
    /// All admin-only actions. Every action checks the session role = Admin.
    /// </summary>
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db) => _db = db;

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

            ViewBag.TotalUsers    = await _db.Users.CountAsync();
            ViewBag.TotalClasses  = await _db.Classes.CountAsync();
            ViewBag.TotalStudents = await _db.Users.CountAsync(u => u.Role == UserRole.Student);
            ViewBag.TotalTeachers = await _db.Users.CountAsync(u => u.Role == UserRole.Teacher);
            ViewBag.RecentLogs    = await _db.ActivityLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.OccurredAt)
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
            var users = await _db.Users.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            if (RequireAdmin() is { } redirect) return redirect;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            if (!ModelState.IsValid) return View(model);

            if (await _db.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already taken.");
                return View(model);
            }
            if (await _db.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already in use.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName     = model.FullName,
                Username     = model.Username,
                Email        = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 11),
                Role         = model.Role,
                IsActive     = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Account for '{model.FullName}' created successfully.";
            return RedirectToAction("Users");
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            var vm = new EditUserViewModel
            {
                Id       = user.Id,
                FullName = user.FullName,
                Email    = user.Email,
                Role     = user.Role,
                IsActive = user.IsActive
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FindAsync(model.Id);
            if (user == null) return NotFound();

            // Prevent removing the last admin
            if (user.Role == UserRole.Admin && model.Role != UserRole.Admin)
            {
                var adminCount = await _db.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive);
                if (adminCount <= 1)
                {
                    ModelState.AddModelError("", "Cannot demote the last active admin.");
                    return View(model);
                }
            }

            user.FullName = model.FullName;
            user.Email    = model.Email;
            user.Role     = model.Role;
            user.IsActive = model.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = "User updated.";
            return RedirectToAction("Users");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.PasswordHash        = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 11);
            user.FailedLoginAttempts = 0;
            user.LockoutEnd          = null;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Password for '{user.FullName}' reset.";
            return RedirectToAction("Users");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(int id)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.FailedLoginAttempts = 0;
            user.LockoutEnd          = null;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Account '{user.Username}' unlocked.";
            return RedirectToAction("Users");
        }

        // ════════════════════════════════════════════════════════════════════
        // Class Management
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Classes()
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var classes = await _db.Classes
                .Include(c => c.ClassTeachers).ThenInclude(ct => ct.Teacher)
                .Include(c => c.ClassStudents)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(classes);
        }

        [HttpGet]
        public IActionResult CreateClass()
        {
            if (RequireAdmin() is { } redirect) return redirect;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass(CreateClassViewModel model)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            if (!ModelState.IsValid) return View(model);

            var cls = new Class
            {
                Name        = model.Name,
                Description = model.Description
            };
            _db.Classes.Add(cls);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Class '{cls.Name}' created.";
            return RedirectToAction("ClassDetail", new { id = cls.Id });
        }

        public async Task<IActionResult> ClassDetail(int id)
        {
            if (RequireAdmin() is { } redirect) return redirect;

            var cls = await _db.Classes
                .Include(c => c.ClassTeachers).ThenInclude(ct => ct.Teacher)
                .Include(c => c.ClassStudents).ThenInclude(cs => cs.Student)
                .Include(c => c.Lessons)
                .Include(c => c.Assignments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cls == null) return NotFound();

            var vm = new ClassDetailViewModel
            {
                Class    = cls,
                Teachers = cls.ClassTeachers.Select(ct => ct.Teacher).ToList(),
                Students = cls.ClassStudents.Select(cs => cs.Student).ToList(),
                Lessons  = cls.Lessons.OrderByDescending(l => l.CreatedAt).ToList(),
                Assignments = cls.Assignments.OrderByDescending(a => a.DueDate).ToList()
            };
            return View(vm);
        }

        // Add Teacher to class
        [HttpGet]
        public async Task<IActionResult> AddTeacher(int classId)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var cls = await _db.Classes.FindAsync(classId);
            if (cls == null) return NotFound();

            var alreadyIds = await _db.ClassTeachers
                .Where(ct => ct.ClassId == classId)
                .Select(ct => ct.TeacherId)
                .ToListAsync();

            var vm = new AddMemberViewModel
            {
                ClassId       = classId,
                ClassName     = cls.Name,
                MemberType    = "Teacher",
                AvailableUsers = await _db.Users
                    .Where(u => u.Role == UserRole.Teacher && u.IsActive && !alreadyIds.Contains(u.Id))
                    .OrderBy(u => u.FullName)
                    .ToListAsync()
            };
            return View("AddMember", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher(int classId, int selectedUserId)
        {
            if (RequireAdmin() is { } redirect) return redirect;

            var exists = await _db.ClassTeachers
                .AnyAsync(ct => ct.ClassId == classId && ct.TeacherId == selectedUserId);
            if (!exists)
            {
                _db.ClassTeachers.Add(new ClassTeacher { ClassId = classId, TeacherId = selectedUserId });
                await _db.SaveChangesAsync();
                TempData["Success"] = "Teacher added to class.";
            }
            return RedirectToAction("ClassDetail", new { id = classId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTeacher(int classId, int teacherId)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var ct = await _db.ClassTeachers
                .FirstOrDefaultAsync(x => x.ClassId == classId && x.TeacherId == teacherId);
            if (ct != null) { _db.ClassTeachers.Remove(ct); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Teacher removed from class.";
            return RedirectToAction("ClassDetail", new { id = classId });
        }

        // Add Student to class
        [HttpGet]
        public async Task<IActionResult> AddStudent(int classId)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var cls = await _db.Classes.FindAsync(classId);
            if (cls == null) return NotFound();

            var alreadyIds = await _db.ClassStudents
                .Where(cs => cs.ClassId == classId)
                .Select(cs => cs.StudentId)
                .ToListAsync();

            var vm = new AddMemberViewModel
            {
                ClassId       = classId,
                ClassName     = cls.Name,
                MemberType    = "Student",
                AvailableUsers = await _db.Users
                    .Where(u => u.Role == UserRole.Student && u.IsActive && !alreadyIds.Contains(u.Id))
                    .OrderBy(u => u.FullName)
                    .ToListAsync()
            };
            return View("AddMember", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(int classId, int selectedUserId)
        {
            if (RequireAdmin() is { } redirect) return redirect;

            var exists = await _db.ClassStudents
                .AnyAsync(cs => cs.ClassId == classId && cs.StudentId == selectedUserId);
            if (!exists)
            {
                _db.ClassStudents.Add(new ClassStudent { ClassId = classId, StudentId = selectedUserId });
                await _db.SaveChangesAsync();
                TempData["Success"] = "Student enrolled in class.";
            }
            return RedirectToAction("ClassDetail", new { id = classId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int classId, int studentId)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            var cs = await _db.ClassStudents
                .FirstOrDefaultAsync(x => x.ClassId == classId && x.StudentId == studentId);
            if (cs != null) { _db.ClassStudents.Remove(cs); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Student removed from class.";
            return RedirectToAction("ClassDetail", new { id = classId });
        }

        // ════════════════════════════════════════════════════════════════════
        // Activity Logs
        // ════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> ActivityLogs(int page = 1)
        {
            if (RequireAdmin() is { } redirect) return redirect;
            const int pageSize = 30;
            var logs = await _db.ActivityLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.Page      = page;
            ViewBag.TotalLogs = await _db.ActivityLogs.CountAsync();
            ViewBag.PageSize  = pageSize;
            return View(logs);
        }
    }
}

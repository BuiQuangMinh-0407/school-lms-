using System.ComponentModel.DataAnnotations;
using SchoolManagement.Models.DBNew2026;

namespace SchoolManagement.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email / Tên đăng nhập")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;
    }

    public class CreateUserViewModel
    {
        [Required, MaxLength(100), Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100), Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Mã GV / Mã SV (tùy chọn)")]
        public string? Code { get; set; }

        [Required, MinLength(6), DataType(DataType.Password), Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare("Password"), Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required, Display(Name = "Vai trò")]
        public string Role { get; set; } = "SinhVien"; // "Admin", "GiangVien", "SinhVien"
    }

    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(100), Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100), Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, Display(Name = "Vai trò")]
        public string Role { get; set; } = "SinhVien";

        [Display(Name = "Tài khoản bị khóa")]
        public bool IsLocked { get; set; }
    }

    public class CreateClassViewModel
    {
        [Required, MaxLength(20), Display(Name = "Mã lớp")]
        public string MaLop { get; set; } = string.Empty;

        [Required, MaxLength(150), Display(Name = "Tên lớp")]
        public string TenLop { get; set; } = string.Empty;

        [MaxLength(20), Display(Name = "Năm học")]
        public string NamHoc { get; set; } = "2026-2027";

        [Display(Name = "Học kỳ")]
        public int HocKy { get; set; } = 1;

        [Display(Name = "Sĩ số tối đa")]
        public int SiSoToiDa { get; set; } = 60;
    }

    public class ClassDetailViewModel
    {
        public LopHoc Class { get; set; } = null!;
        public List<GiangVien> Teachers { get; set; } = new();
        public List<SinhVien> Students { get; set; } = new();
        public List<BaiHoc> Lessons { get; set; } = new();
        public List<BaiTap> Assignments { get; set; } = new();
    }

    public class AddMemberViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string MemberType { get; set; } = "Student"; // "Teacher" | "Student"
        public List<SelectMemberItem> AvailableUsers { get; set; } = new();
        public int SelectedUserId { get; set; }
        public string? RoleInClass { get; set; } = "Giảng viên chính";
    }

    public class SelectMemberItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class CreateLessonViewModel
    {
        public int ClassId { get; set; }
        public int LessonId { get; set; }

        [Required, MaxLength(200), Display(Name = "Tiêu đề bài học")]
        public string Title { get; set; } = string.Empty;

        [Required, Display(Name = "Nội dung bài học")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Xuất bản ngay")]
        public bool IsPublished { get; set; }
    }

    public class CreateAssignmentViewModel
    {
        public int ClassId { get; set; }
        public int AssignmentId { get; set; }

        [Required, MaxLength(200), Display(Name = "Tiêu đề bài tập")]
        public string Title { get; set; } = string.Empty;

        [Required, Display(Name = "Mô tả / Yêu cầu")]
        public string Description { get; set; } = string.Empty;

        [Required, DataType(DataType.DateTime), Display(Name = "Hạn nộp")]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Xuất bản ngay")]
        public bool IsPublished { get; set; } = true;
    }

    public class SubmitAssignmentViewModel
    {
        public int AssignmentId { get; set; }
        public string AssignmentTitle { get; set; } = string.Empty;
        public string AssignmentDescription { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung bài nộp")]
        [Display(Name = "Nội dung bài làm")]
        public string SubmissionContent { get; set; } = string.Empty;
    }

    public class GradeSubmissionViewModel
    {
        public int SubmissionId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string SubmissionContent { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }

        [Required, Range(0, 10), Display(Name = "Điểm (thang 10)")]
        public decimal Grade { get; set; }

        [MaxLength(500), Display(Name = "Nhận xét")]
        public string? Feedback { get; set; }
    }
}

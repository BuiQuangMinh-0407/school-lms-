using System;
using System.Collections.Generic;

namespace SchoolManagement.Models.DBNew2026;

public partial class SinhVien
{
    public int SinhVienId { get; set; }

    public string MaSinhVien { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime? NgaySinh { get; set; }

    public int? TaiKhoanId { get; set; }

    public virtual ICollection<BaiNop> BaiNops { get; set; } = new List<BaiNop>();

    public virtual ICollection<SinhVienLop> SinhVienLops { get; set; } = new List<SinhVienLop>();

    public virtual TaiKhoan? TaiKhoan { get; set; }
}

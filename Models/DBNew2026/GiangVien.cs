using System;
using System.Collections.Generic;

namespace SchoolManagement.Models.DBNew2026;

public partial class GiangVien
{
    public int GiangVienId { get; set; }

    public string MaGiangVien { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string BoMon { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public int? TaiKhoanId { get; set; }

    public virtual ICollection<PhanCongGiangDay> PhanCongGiangDays { get; set; } = new List<PhanCongGiangDay>();

    public virtual TaiKhoan? TaiKhoan { get; set; }
}

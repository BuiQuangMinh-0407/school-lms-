using System;
using System.Collections.Generic;

namespace SchoolManagement.Models.DBNew2026;

public partial class BaiNop
{
    public int BaiNopId { get; set; }

    public int BaiTapId { get; set; }

    public int SinhVienId { get; set; }

    public string NoiDung { get; set; } = null!;

    public DateTime NgayNop { get; set; }

    public decimal? Diem { get; set; }

    public string? NhanXet { get; set; }

    public virtual BaiTap BaiTap { get; set; } = null!;

    public virtual SinhVien SinhVien { get; set; } = null!;
}

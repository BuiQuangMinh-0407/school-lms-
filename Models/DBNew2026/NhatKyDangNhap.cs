using System;
using System.Collections.Generic;

namespace SchoolManagement.Models.DBNew2026;

public partial class NhatKyDangNhap
{
    public int NhatKyId { get; set; }

    public string Email { get; set; } = null!;

    public bool ThanhCong { get; set; }

    public string LyDo { get; set; } = null!;

    public string? DiaChiIp { get; set; }

    public DateTime ThoiDiem { get; set; }

    public int? TaiKhoanId { get; set; }
}

using System;
using System.Collections.Generic;

namespace SchoolManagement.Models.DBNew2026;

public partial class TaiKhoan
{
    public int TaiKhoanId { get; set; }

    public string Email { get; set; } = null!;

    public string MatKhauHash { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string VaiTro { get; set; } = null!;

    public bool BiKhoa { get; set; }

    public int SoLanSai { get; set; }

    public DateTime? KhoaDen { get; set; }

    public virtual ICollection<GiangVien> GiangViens { get; set; } = new List<GiangVien>();

    public virtual ICollection<SinhVien> SinhViens { get; set; } = new List<SinhVien>();
}

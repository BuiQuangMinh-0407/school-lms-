using System;
using System.Collections.Generic;

namespace SchoolManagement.Models.DBNew2026;

public partial class SinhVienLop
{
    public int SinhVienId { get; set; }

    public int LopHocId { get; set; }

    public DateTime NgayVaoLop { get; set; }

    public virtual LopHoc LopHoc { get; set; } = null!;

    public virtual SinhVien SinhVien { get; set; } = null!;
}

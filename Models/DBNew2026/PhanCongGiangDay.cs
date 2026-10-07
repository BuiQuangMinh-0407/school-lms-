using System;
using System.Collections.Generic;

namespace SchoolManagement.Models.DBNew2026;

public partial class PhanCongGiangDay
{
    public int PhanCongId { get; set; }

    public int GiangVienId { get; set; }

    public int LopHocId { get; set; }

    public string VaiTro { get; set; } = null!;

    public virtual GiangVien GiangVien { get; set; } = null!;

    public virtual LopHoc LopHoc { get; set; } = null!;
}

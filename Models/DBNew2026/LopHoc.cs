using System;
using System.Collections.Generic;

namespace SchoolManagement.Models.DBNew2026;

public partial class LopHoc
{
    public int LopHocId { get; set; }

    public string MaLop { get; set; } = null!;

    public string TenLop { get; set; } = null!;

    public string NamHoc { get; set; } = null!;

    public int HocKy { get; set; }

    public int SiSoToiDa { get; set; }

    public virtual ICollection<BaiHoc> BaiHocs { get; set; } = new List<BaiHoc>();

    public virtual ICollection<BaiTap> BaiTaps { get; set; } = new List<BaiTap>();

    public virtual ICollection<PhanCongGiangDay> PhanCongGiangDays { get; set; } = new List<PhanCongGiangDay>();

    public virtual ICollection<SinhVienLop> SinhVienLops { get; set; } = new List<SinhVienLop>();
}

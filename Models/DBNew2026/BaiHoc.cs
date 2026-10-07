using System;
using System.Collections.Generic;

namespace SchoolManagement.Models.DBNew2026;

public partial class BaiHoc
{
    public int BaiHocId { get; set; }

    public int LopHocId { get; set; }

    public string TieuDe { get; set; } = null!;

    public string NoiDung { get; set; } = null!;

    public bool DaXuatBan { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? NgayXuatBan { get; set; }

    public virtual LopHoc LopHoc { get; set; } = null!;
}

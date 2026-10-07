using System;
using System.Collections.Generic;

namespace SchoolManagement.Models.DBNew2026;

public partial class BaiTap
{
    public int BaiTapId { get; set; }

    public int LopHocId { get; set; }

    public string TieuDe { get; set; } = null!;

    public string MoTa { get; set; } = null!;

    public DateTime? HanNop { get; set; }

    public bool DaXuatBan { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? NgayXuatBan { get; set; }

    public virtual ICollection<BaiNop> BaiNops { get; set; } = new List<BaiNop>();

    public virtual LopHoc LopHoc { get; set; } = null!;
}

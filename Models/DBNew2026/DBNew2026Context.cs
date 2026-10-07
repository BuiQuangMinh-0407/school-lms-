using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SchoolManagement.Models.DBNew2026;

public partial class DBNew2026Context : DbContext
{
    public DBNew2026Context()
    {
    }

    public DBNew2026Context(DbContextOptions<DBNew2026Context> options)
        : base(options)
    {
    }

    public virtual DbSet<BaiHoc> BaiHocs { get; set; }

    public virtual DbSet<BaiNop> BaiNops { get; set; }

    public virtual DbSet<BaiTap> BaiTaps { get; set; }

    public virtual DbSet<GiangVien> GiangViens { get; set; }

    public virtual DbSet<LopHoc> LopHocs { get; set; }

    public virtual DbSet<NhatKyDangNhap> NhatKyDangNhaps { get; set; }

    public virtual DbSet<PhanCongGiangDay> PhanCongGiangDays { get; set; }

    public virtual DbSet<SinhVien> SinhViens { get; set; }

    public virtual DbSet<SinhVienLop> SinhVienLops { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=DBNew2026;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BaiHoc>(entity =>
        {
            entity.HasKey(e => e.BaiHocId).HasName("PK__BaiHoc__59827F3A6A5936CB");

            entity.ToTable("BaiHoc");

            entity.Property(e => e.TieuDe).HasMaxLength(200);

            entity.HasOne(d => d.LopHoc).WithMany(p => p.BaiHocs)
                .HasForeignKey(d => d.LopHocId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BaiHoc_LopHoc");
        });

        modelBuilder.Entity<BaiNop>(entity =>
        {
            entity.HasKey(e => e.BaiNopId).HasName("PK__BaiNop__B71B8A9827F87497");

            entity.ToTable("BaiNop");

            entity.HasIndex(e => new { e.BaiTapId, e.SinhVienId }, "UQ_BaiNop").IsUnique();

            entity.Property(e => e.Diem).HasColumnType("decimal(4, 1)");
            entity.Property(e => e.NhanXet).HasMaxLength(500);

            entity.HasOne(d => d.BaiTap).WithMany(p => p.BaiNops)
                .HasForeignKey(d => d.BaiTapId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BaiNop_BaiTap");

            entity.HasOne(d => d.SinhVien).WithMany(p => p.BaiNops)
                .HasForeignKey(d => d.SinhVienId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BaiNop_SinhVien");
        });

        modelBuilder.Entity<BaiTap>(entity =>
        {
            entity.HasKey(e => e.BaiTapId).HasName("PK__BaiTap__48494B45AE977E68");

            entity.ToTable("BaiTap");

            entity.Property(e => e.TieuDe).HasMaxLength(200);

            entity.HasOne(d => d.LopHoc).WithMany(p => p.BaiTaps)
                .HasForeignKey(d => d.LopHocId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BaiTap_LopHoc");
        });

        modelBuilder.Entity<GiangVien>(entity =>
        {
            entity.HasKey(e => e.GiangVienId).HasName("PK__GiangVie__626127E29FAD4CDD");

            entity.ToTable("GiangVien");

            entity.HasIndex(e => e.MaGiangVien, "UQ_GiangVien_Ma").IsUnique();

            entity.Property(e => e.BoMon).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MaGiangVien).HasMaxLength(20);
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.GiangViens)
                .HasForeignKey(d => d.TaiKhoanId)
                .HasConstraintName("FK_GiangVien_TaiKhoan");
        });

        modelBuilder.Entity<LopHoc>(entity =>
        {
            entity.HasKey(e => e.LopHocId).HasName("PK__LopHoc__DBC49620D7255BAA");

            entity.ToTable("LopHoc");

            entity.HasIndex(e => e.MaLop, "UQ_LopHoc_Ma").IsUnique();

            entity.Property(e => e.MaLop).HasMaxLength(20);
            entity.Property(e => e.NamHoc).HasMaxLength(20);
            entity.Property(e => e.TenLop).HasMaxLength(150);
        });

        modelBuilder.Entity<NhatKyDangNhap>(entity =>
        {
            entity.HasKey(e => e.NhatKyId).HasName("PK__NhatKyDa__DF38EBAF7739C893");

            entity.ToTable("NhatKyDangNhap");

            entity.Property(e => e.DiaChiIp).HasMaxLength(64);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.LyDo).HasMaxLength(50);
        });

        modelBuilder.Entity<PhanCongGiangDay>(entity =>
        {
            entity.HasKey(e => e.PhanCongId).HasName("PK__PhanCong__7EF840BDD203116A");

            entity.ToTable("PhanCongGiangDay");

            entity.HasIndex(e => new { e.GiangVienId, e.LopHocId }, "UQ_PhanCong").IsUnique();

            entity.Property(e => e.VaiTro).HasMaxLength(50);

            entity.HasOne(d => d.GiangVien).WithMany(p => p.PhanCongGiangDays)
                .HasForeignKey(d => d.GiangVienId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhanCong_GiangVien");

            entity.HasOne(d => d.LopHoc).WithMany(p => p.PhanCongGiangDays)
                .HasForeignKey(d => d.LopHocId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhanCong_LopHoc");
        });

        modelBuilder.Entity<SinhVien>(entity =>
        {
            entity.HasKey(e => e.SinhVienId).HasName("PK__SinhVien__F3CF814EC93BC8F4");

            entity.ToTable("SinhVien");

            entity.HasIndex(e => e.MaSinhVien, "UQ_SinhVien_Ma").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MaSinhVien).HasMaxLength(20);

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.SinhViens)
                .HasForeignKey(d => d.TaiKhoanId)
                .HasConstraintName("FK_SinhVien_TaiKhoan");
        });

        modelBuilder.Entity<SinhVienLop>(entity =>
        {
            entity.HasKey(e => new { e.SinhVienId, e.LopHocId });

            entity.ToTable("SinhVienLop");

            entity.HasOne(d => d.LopHoc).WithMany(p => p.SinhVienLops)
                .HasForeignKey(d => d.LopHocId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SinhVienLop_LopHoc");

            entity.HasOne(d => d.SinhVien).WithMany(p => p.SinhVienLops)
                .HasForeignKey(d => d.SinhVienId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SinhVienLop_SinhVien");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.TaiKhoanId).HasName("PK__TaiKhoan__9A124B45700221E2");

            entity.ToTable("TaiKhoan");

            entity.HasIndex(e => e.Email, "UQ_TaiKhoan_Email").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MatKhauHash).HasMaxLength(500);
            entity.Property(e => e.VaiTro).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

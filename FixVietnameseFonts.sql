USE DBNew2026;
GO

-- 1. TaiKhoan
UPDATE TaiKhoan SET HoTen = N'Quản trị hệ thống' WHERE Email = 'admin@neu.edu.vn';
UPDATE TaiKhoan SET HoTen = N'Nguyễn Văn An' WHERE Email = 'gv.an@neu.edu.vn';
UPDATE TaiKhoan SET HoTen = N'Trần Thị Bình' WHERE Email = 'gv.binh@neu.edu.vn';
UPDATE TaiKhoan SET HoTen = N'Lê Thị Lan' WHERE Email = 'sv.lan@neu.edu.vn';
UPDATE TaiKhoan SET HoTen = N'Phạm Minh' WHERE Email = 'sv.minh@neu.edu.vn';
UPDATE TaiKhoan SET HoTen = N'Hoàng Hà' WHERE Email = 'sv.ha@neu.edu.vn';

-- 2. GiangVien
UPDATE GiangVien SET HoTen = N'Nguyễn Văn An', BoMon = N'Công nghệ thông tin' WHERE Email = 'gv.an@neu.edu.vn';
UPDATE GiangVien SET HoTen = N'Trần Thị Bình', BoMon = N'Hệ thống thông tin' WHERE Email = 'gv.binh@neu.edu.vn';

-- 3. SinhVien
UPDATE SinhVien SET HoTen = N'Lê Thị Lan' WHERE Email = 'sv.lan@neu.edu.vn';
UPDATE SinhVien SET HoTen = N'Phạm Minh' WHERE Email = 'sv.minh@neu.edu.vn';
UPDATE SinhVien SET HoTen = N'Hoàng Hà' WHERE Email = 'sv.ha@neu.edu.vn';

-- 4. LopHoc
UPDATE LopHoc SET TenLop = N'Bảo mật phần mềm', NamHoc = N'2025–2026' WHERE MaLop = 'BMTT01';
UPDATE LopHoc SET TenLop = N'Cơ sở dữ liệu', NamHoc = N'2025–2026' WHERE MaLop = 'CSDL01';

-- 5. PhanCongGiangDay
UPDATE PhanCongGiangDay SET VaiTro = N'Giảng viên chính';

-- 6. BaiHoc
UPDATE BaiHoc SET 
    TieuDe = N'Xác thực và phân quyền',
    NoiDung = N'Bài đã xuất bản. Sinh viên trong lớp BMTT01 xem được. Giảng viên lớp khác không vào được.'
WHERE BaiHocId = 1;

UPDATE BaiHoc SET 
    TieuDe = N'Nháp: Khóa tài khoản',
    NoiDung = N'Bài chưa xuất bản. Sinh viên không thấy mục này.'
WHERE BaiHocId = 2;

UPDATE BaiHoc SET 
    TieuDe = N'Ràng buộc toàn vẹn',
    NoiDung = N'Bài của lớp CSDL01. Sinh viên lớp BMTT01 không xem được.'
WHERE BaiHocId = 3;

-- 7. BaiTap
UPDATE BaiTap SET 
    TieuDe = N'Bài tập đăng nhập an toàn',
    MoTa = N'Trình bày cách băm mật khẩu, giới hạn số lần đăng nhập sai và ghi nhật ký.'
WHERE BaiTapId = 1;

-- 8. BaiNop
UPDATE BaiNop SET 
    NoiDung = N'Mật khẩu lưu bằng PBKDF2. Sai 5 lần thì tạm khóa 10 phút. Mỗi lần đăng nhập đều ghi nhật ký.',
    NhanXet = N'Đủ ý, trình bày rõ.'
WHERE BaiNopId = 1;
GO

SELECT 'TaiKhoan' as Tbl, HoTen FROM TaiKhoan;
SELECT 'LopHoc' as Tbl, MaLop, TenLop FROM LopHoc;
GO

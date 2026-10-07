USE DBNew2026;
GO

-- Real PBKDF2 hash for "Admin@123"
UPDATE TaiKhoan 
SET MatKhauHash = 'PBKDF2$100000$lRP4+aOMigx4DgKqd/kSUw==$xuqgZv6hSkwAOlX1NnD5aVUmCUiAimgc3biJ9HZQ0T4=',
    BiKhoa = 0,
    SoLanSai = 0,
    KhoaDen = NULL;
GO

SELECT TaiKhoanId, Email, MatKhauHash, SoLanSai, BiKhoa FROM TaiKhoan;
GO

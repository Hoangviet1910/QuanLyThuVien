using Microsoft.EntityFrameworkCore;

namespace QuanLyThuVien.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaiKhoan> TaiKhoans { get; set; }
        public DbSet<TheLoai> TheLoais { get; set; }
        public DbSet<NhaXuatBan> NhaXuatBans { get; set; }
        public DbSet<Sach> Sachs { get; set; }
        public DbSet<BanDoc> BanDocs { get; set; }
        public DbSet<PhieuMuon> PhieuMuons { get; set; }
        public DbSet<ChiTietMuon> ChiTietMuons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Seed data for Admin account
            modelBuilder.Entity<TaiKhoan>().HasData(
                new TaiKhoan
                {
                    MaTaiKhoan = 1,
                    TenDangNhap = "admin",
                    MatKhau = "123456", // In production this should be hashed
                    HoTen = "Administrator",
                    VaiTro = 1,
                    TrangThai = true
                }
            );

            // Seed some data for TheLoai
            modelBuilder.Entity<TheLoai>().HasData(
                new TheLoai { MaTheLoai = 1, TenTheLoai = "Tiểu thuyết", ThuTuHienThi = 1, TrangThai = true },
                new TheLoai { MaTheLoai = 2, TenTheLoai = "Khoa học", ThuTuHienThi = 2, TrangThai = true },
                new TheLoai { MaTheLoai = 3, TenTheLoai = "Văn học", ThuTuHienThi = 3, TrangThai = true },
                new TheLoai { MaTheLoai = 4, TenTheLoai = "Lịch sử", ThuTuHienThi = 4, TrangThai = true },
                new TheLoai { MaTheLoai = 5, TenTheLoai = "Công nghệ", ThuTuHienThi = 5, TrangThai = true }
            );

            // Prevent cascading delete cycles
            modelBuilder.Entity<PhieuMuon>()
                .HasOne(p => p.BanDoc)
                .WithMany(b => b.PhieuMuons)
                .HasForeignKey(p => p.MaBanDoc)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChiTietMuon>()
                .HasOne(c => c.PhieuMuon)
                .WithMany(p => p.ChiTietMuons)
                .HasForeignKey(c => c.MaPhieuMuon)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<ChiTietMuon>()
                .HasOne(c => c.Sach)
                .WithMany(s => s.ChiTietMuons)
                .HasForeignKey(c => c.MaSach)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

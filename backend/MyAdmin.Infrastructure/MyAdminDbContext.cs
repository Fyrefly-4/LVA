using Microsoft.EntityFrameworkCore;
using MyAdmin.Core.Entities;

namespace MyAdmin.Infrastructure;

public class MyAdminDbContext : DbContext
{
    public MyAdminDbContext(DbContextOptions<MyAdminDbContext> options) : base(options)
    {
    }

    public DbSet<SysUser> SysUsers => Set<SysUser>();
    public DbSet<SysRole> SysRoles => Set<SysRole>();
    public DbSet<SysUserRole> SysUserRoles => Set<SysUserRole>();
    public DbSet<SysMenu> SysMenus => Set<SysMenu>();
    public DbSet<SysRoleMenu> SysRoleMenus => Set<SysRoleMenu>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SysUser>(entity =>
        {
            entity.ToTable("SysUser");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Nickname).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Status).HasDefaultValue((byte)1);
            entity.Property(e => e.CreateTime).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<SysRole>(entity =>
        {
            entity.ToTable("SysRole");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.RoleName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RoleCode).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.RoleCode).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.CreateTime).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<SysUserRole>(entity =>
        {
            entity.ToTable("SysUserRole");
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SysMenu>(entity =>
        {
            entity.ToTable("SysMenu");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Path).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Component).HasMaxLength(200);
            entity.Property(e => e.PermCode).HasMaxLength(100);
            entity.Property(e => e.MenuType).IsRequired();
            entity.Property(e => e.Icon).HasMaxLength(100);
            entity.Property(e => e.Sort).HasDefaultValue(0);
            entity.Property(e => e.Status).HasDefaultValue((byte)1);
            entity.Property(e => e.CreateTime).HasDefaultValueSql("GETDATE()");
            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => e.PermCode);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SysRoleMenu>(entity =>
        {
            entity.ToTable("SysRoleMenu");
            entity.HasKey(e => new { e.RoleId, e.MenuId });

            entity.HasOne(e => e.Role)
                .WithMany(r => r.RoleMenus)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Menu)
                .WithMany(m => m.RoleMenus)
                .HasForeignKey(e => e.MenuId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.MenuId);
        });
    }
}

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SplitMate.Domain.Entities;
using SplitMate.Infrastructure.Identity;

namespace SplitMate.Infrastructure.Persistence;

public class SplitMateDbContext(DbContextOptions<SplitMateDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseShare> ExpenseShares => Set<ExpenseShare>();
    public DbSet<Settlement> Settlements => Set<Settlement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(b =>
            b.Property(u => u.DisplayName).HasMaxLength(100).IsRequired());

        builder.Entity<Group>(b =>
        {
            b.Property(g => g.Name).HasMaxLength(100).IsRequired();
            b.Property(g => g.CreatedByUserId).HasMaxLength(450).IsRequired();
            b.HasOne<AppUser>().WithMany()
                .HasForeignKey(g => g.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GroupMember>(b =>
        {
            b.HasKey(m => new { m.GroupId, m.UserId });
            b.Property(m => m.UserId).HasMaxLength(450);
            b.HasOne(m => m.Group).WithMany(g => g.Members)
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<AppUser>().WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Expense>(b =>
        {
            b.Property(e => e.Description).HasMaxLength(200).IsRequired();
            b.Property(e => e.Amount).HasPrecision(18, 2);
            b.Property(e => e.PaidByUserId).HasMaxLength(450).IsRequired();
            b.HasOne(e => e.Group).WithMany(g => g.Expenses)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<AppUser>().WithMany()
                .HasForeignKey(e => e.PaidByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExpenseShare>(b =>
        {
            b.HasKey(s => new { s.ExpenseId, s.UserId });
            b.Property(s => s.UserId).HasMaxLength(450);
            b.Property(s => s.ShareAmount).HasPrecision(18, 2);
            b.HasOne(s => s.Expense).WithMany(e => e.Shares)
                .HasForeignKey(s => s.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<AppUser>().WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Settlement>(b =>
        {
            b.Property(s => s.Amount).HasPrecision(18, 2);
            b.Property(s => s.Note).HasMaxLength(200);
            b.Property(s => s.FromUserId).HasMaxLength(450).IsRequired();
            b.Property(s => s.ToUserId).HasMaxLength(450).IsRequired();
            b.HasOne(s => s.Group).WithMany(g => g.Settlements)
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<AppUser>().WithMany()
                .HasForeignKey(s => s.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<AppUser>().WithMany()
                .HasForeignKey(s => s.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

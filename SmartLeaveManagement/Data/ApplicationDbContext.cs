namespace SmartLeaveManagement.Data;

using Microsoft.EntityFrameworkCore;
using SmartLeaveManagement.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure LeaveRequest relationships and indexes
        modelBuilder.Entity<LeaveRequest>()
            .HasOne(lr => lr.Employee)
            .WithMany()
            .HasForeignKey(lr => lr.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeaveRequest>()
            .HasOne(lr => lr.ApprovedByUser)
            .WithMany()
            .HasForeignKey(lr => lr.ApprovedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Create index for performance on frequently queried columns
        modelBuilder.Entity<LeaveRequest>()
            .HasIndex(lr => new { lr.EmployeeId, lr.Status })
            .HasName("IX_LeaveRequests_EmployeeId_Status");

        modelBuilder.Entity<LeaveRequest>()
            .HasIndex(lr => lr.Status)
            .HasName("IX_LeaveRequests_Status");
    }
}


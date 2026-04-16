using Microsoft.EntityFrameworkCore;
using PharmacyServer.Models;

namespace PharmacyServer.Models;

public partial class PharmacyDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Явная конфигурация отношений для ComponentRequest и Employee
        modelBuilder.Entity<ComponentRequest>(entity =>
        {
            entity.HasOne(d => d.RequestedByEmployee)
                .WithMany(p => p.ComponentRequestRequestedByEmployees)
                .HasForeignKey(d => d.RequestedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ComponentRequests_RequestingEmployee");

            entity.HasOne(d => d.ApprovedByEmployee)
                .WithMany(p => p.ComponentRequestApprovedByEmployees)
                .HasForeignKey(d => d.ApprovedByEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ComponentRequests_ApprovingEmployee");
        });

        // Конфигурация отношений для InventoryCheck и Employee
        modelBuilder.Entity<InventoryCheck>(entity =>
        {
            entity.HasOne(d => d.ConductedByEmployee)
                .WithMany(p => p.InventoryCheckConductedByEmployees)
                .HasForeignKey(d => d.ConductedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_InventoryChecks_ConductedBy");

            entity.HasOne(d => d.CheckedByEmployee)
                .WithMany(p => p.InventoryCheckCheckedByEmployees)
                .HasForeignKey(d => d.CheckedByEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_InventoryChecks_CheckedBy");
        });

        // Конфигурация отношений для Order и Employee
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasOne(d => d.AssignedEmployee)
                .WithMany(p => p.OrderAssignedEmployees)
                .HasForeignKey(d => d.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Orders_AssignedEmployee");

            entity.HasOne(d => d.ProductionEmployee)
                .WithMany(p => p.OrderProductionEmployees)
                .HasForeignKey(d => d.ProductionEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Orders_ProductionEmployee");
        });
    }
}

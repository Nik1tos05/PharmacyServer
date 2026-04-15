using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PharmacyServer.Models;

public partial class PharmacyDbContext : DbContext
{
    public PharmacyDbContext()
    {
    }

    public PharmacyDbContext(DbContextOptions<PharmacyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Component> Components { get; set; }

    public virtual DbSet<ComponentRequest> ComponentRequests { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<InventoryCheck> InventoryChecks { get; set; }

    public virtual DbSet<InventoryDetail> InventoryDetails { get; set; }

    public virtual DbSet<Medicine> Medicines { get; set; }

    public virtual DbSet<MedicineCategory> MedicineCategories { get; set; }

    public virtual DbSet<MedicineComposition> MedicineCompositions { get; set; }

    public virtual DbSet<MedicineType> MedicineTypes { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<PreparationTechnology> PreparationTechnologies { get; set; }

    public virtual DbSet<Prescription> Prescriptions { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<UnitsOfMeasure> UnitsOfMeasures { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=PharmacyDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Component>(entity =>
        {
            entity.HasKey(e => e.ComponentId).HasName("PK__Componen__D79CF02E6D769C31");

            entity.HasIndex(e => new { e.CurrentStock, e.CriticalNorm }, "IX_Components_CriticalNorm");

            entity.HasIndex(e => e.CurrentStock, "IX_Components_Stock");

            entity.HasIndex(e => e.ComponentName, "UQ__Componen__DB06D1C106B152CC").IsUnique();

            entity.Property(e => e.ComponentId).HasColumnName("ComponentID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.ComponentName).HasMaxLength(200);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CriticalNorm).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.CurrentStock)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 3)");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StorageConditions).HasMaxLength(500);
            entity.Property(e => e.Supplier).HasMaxLength(200);
            entity.Property(e => e.UnitId).HasColumnName("UnitID");

            entity.HasOne(d => d.Category).WithMany(p => p.Components)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Components_Category");

            entity.HasOne(d => d.Unit).WithMany(p => p.Components)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Components_Unit");
        });

        modelBuilder.Entity<ComponentRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__Componen__33A8519AF1296FE1");

            entity.HasIndex(e => e.ComponentId, "IX_ComponentRequests_Component");

            entity.HasIndex(e => e.RequestDate, "IX_ComponentRequests_Date");

            entity.HasIndex(e => e.RequestStatus, "IX_ComponentRequests_Status");

            entity.HasIndex(e => e.RequestNumber, "UQ__Componen__9ADA6BE02ECD8538").IsUnique();

            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.ActualPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ApprovedByEmployeeId).HasColumnName("ApprovedByEmployeeID");
            entity.Property(e => e.ComponentId).HasColumnName("ComponentID");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DeliveryNotes).HasMaxLength(500);
            entity.Property(e => e.ExpectedPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.RequestDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.RequestNumber).HasMaxLength(50);
            entity.Property(e => e.RequestStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.RequestedByEmployeeId).HasColumnName("RequestedByEmployeeID");
            entity.Property(e => e.RequestedQuantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Supplier).HasMaxLength(200);

            entity.HasOne(d => d.ApprovedByEmployee).WithMany(p => p.ComponentRequestApprovedByEmployees)
                .HasForeignKey(d => d.ApprovedByEmployeeId)
                .HasConstraintName("FK_ComponentRequests_ApprovingEmployee");

            entity.HasOne(d => d.Component).WithMany(p => p.ComponentRequests)
                .HasForeignKey(d => d.ComponentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComponentRequests_Component");

            entity.HasOne(d => d.Order).WithMany(p => p.ComponentRequests)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_ComponentRequests_Order");

            entity.HasOne(d => d.RequestedByEmployee).WithMany(p => p.ComponentRequestRequestedByEmployees)
                .HasForeignKey(d => d.RequestedByEmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComponentRequests_RequestingEmployee");
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.DoctorId).HasName("PK__Doctors__2DC00EDF883990B9");

            entity.HasIndex(e => e.LicenseNumber, "UQ__Doctors__E8890166EC4D4B72").IsUnique();

            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.ClinicName).HasMaxLength(200);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.LicenseNumber).HasMaxLength(50);
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Patronymic).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Specialization).HasMaxLength(100);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04FF18E3C017E");

            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.CanSignDocuments).HasDefaultValue(false);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsManager).HasDefaultValue(false);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PassportNumber).HasMaxLength(10);
            entity.Property(e => e.PassportSeries).HasMaxLength(10);
            entity.Property(e => e.Patronymic).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.Salary).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<InventoryCheck>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK__Inventor__F5FDE6D3125C46FB");

            entity.HasIndex(e => e.CheckDate, "IX_InventoryChecks_Date");

            entity.HasIndex(e => e.Status, "IX_InventoryChecks_Status");

            entity.HasIndex(e => e.InventoryNumber, "UQ__Inventor__D6D65CC8862CBC6D").IsUnique();

            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.CheckDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CheckedByEmployeeId).HasColumnName("CheckedByEmployeeID");
            entity.Property(e => e.ConductedByEmployeeId).HasColumnName("ConductedByEmployeeID");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CriticalNormViolations).HasDefaultValue(0);
            entity.Property(e => e.DiscrepanciesFound).HasDefaultValue(0);
            entity.Property(e => e.ExpiredItemsCount).HasDefaultValue(0);
            entity.Property(e => e.InventoryNumber).HasMaxLength(50);
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.ReportGenerated).HasDefaultValue(false);
            entity.Property(e => e.ShortageValue)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Scheduled");
            entity.Property(e => e.SurplusValue)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalItemsChecked).HasDefaultValue(0);

            entity.HasOne(d => d.CheckedByEmployee).WithMany(p => p.InventoryCheckCheckedByEmployees)
                .HasForeignKey(d => d.CheckedByEmployeeId)
                .HasConstraintName("FK_InventoryChecks_CheckedBy");

            entity.HasOne(d => d.ConductedByEmployee).WithMany(p => p.InventoryCheckConductedByEmployees)
                .HasForeignKey(d => d.ConductedByEmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryChecks_ConductedBy");
        });

        modelBuilder.Entity<InventoryDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId).HasName("PK__Inventor__135C314D3B43A143");

            entity.HasIndex(e => e.IsBelowCriticalNorm, "IX_InventoryDetails_CriticalNorm");

            entity.HasIndex(e => e.IsExpired, "IX_InventoryDetails_Expired");

            entity.Property(e => e.DetailId).HasColumnName("DetailID");
            entity.Property(e => e.ActualQuantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.CheckedByEmployeeId).HasColumnName("CheckedByEmployeeID");
            entity.Property(e => e.Condition).HasMaxLength(100);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Difference).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.ExpectedQuantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
            entity.Property(e => e.IsBelowCriticalNorm).HasDefaultValue(false);
            entity.Property(e => e.IsExpired).HasDefaultValue(false);
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.ItemType).HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.UnitId).HasColumnName("UnitID");

            entity.HasOne(d => d.CheckedByEmployee).WithMany(p => p.InventoryDetails)
                .HasForeignKey(d => d.CheckedByEmployeeId)
                .HasConstraintName("FK_InventoryDetails_Employee");

            entity.HasOne(d => d.Inventory).WithMany(p => p.InventoryDetails)
                .HasForeignKey(d => d.InventoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryDetails_Inventory");

            entity.HasOne(d => d.Unit).WithMany(p => p.InventoryDetails)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryDetails_Unit");
        });

        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.HasKey(e => e.MedicineId).HasName("PK__Medicine__4F2128F086B4F03A");

            entity.HasIndex(e => new { e.CurrentStock, e.CriticalNorm }, "IX_Medicines_CriticalNorm");

            entity.HasIndex(e => e.CurrentStock, "IX_Medicines_Stock");

            entity.Property(e => e.MedicineId).HasColumnName("MedicineID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CriticalNorm).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.CurrentStock)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsReadyMade).HasDefaultValue(true);
            entity.Property(e => e.ManufacturingCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MedicineName).HasMaxLength(200);
            entity.Property(e => e.MedicineTypeId).HasColumnName("MedicineTypeID");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.RequiresPrescription).HasDefaultValue(false);
            entity.Property(e => e.SalePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TechnologyId).HasColumnName("TechnologyID");
            entity.Property(e => e.UnitId).HasColumnName("UnitID");

            entity.HasOne(d => d.Category).WithMany(p => p.Medicines)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Medicines_Category");

            entity.HasOne(d => d.MedicineType).WithMany(p => p.Medicines)
                .HasForeignKey(d => d.MedicineTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Medicines_MedicineType");

            entity.HasOne(d => d.Technology).WithMany(p => p.Medicines)
                .HasForeignKey(d => d.TechnologyId)
                .HasConstraintName("FK_Medicines_Technology");

            entity.HasOne(d => d.Unit).WithMany(p => p.Medicines)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Medicines_Unit");
        });

        modelBuilder.Entity<MedicineCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Medicine__19093A2B2560CD5E");

            entity.HasIndex(e => e.CategoryName, "UQ__Medicine__8517B2E0A6BA1010").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ParentCategoryId).HasColumnName("ParentCategoryID");

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryId)
                .HasConstraintName("FK_MedicineCategories_Parent");
        });

        modelBuilder.Entity<MedicineComposition>(entity =>
        {
            entity.HasKey(e => e.CompositionId).HasName("PK__Medicine__B8E2333FF057C354");

            entity.ToTable("MedicineComposition");

            entity.HasIndex(e => new { e.MedicineId, e.ComponentId }, "UQ_MedicineComposition").IsUnique();

            entity.Property(e => e.CompositionId).HasColumnName("CompositionID");
            entity.Property(e => e.ComponentId).HasColumnName("ComponentID");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MedicineId).HasColumnName("MedicineID");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.UnitId).HasColumnName("UnitID");

            entity.HasOne(d => d.Component).WithMany(p => p.MedicineCompositions)
                .HasForeignKey(d => d.ComponentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MedicineComposition_Component");

            entity.HasOne(d => d.Medicine).WithMany(p => p.MedicineCompositions)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MedicineComposition_Medicine");

            entity.HasOne(d => d.Unit).WithMany(p => p.MedicineCompositions)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MedicineComposition_Unit");
        });

        modelBuilder.Entity<MedicineType>(entity =>
        {
            entity.HasKey(e => e.MedicineTypeId).HasName("PK__Medicine__AB4D17B4B25BFFE0");

            entity.HasIndex(e => e.TypeName, "UQ__Medicine__D4E7DFA8ABF9A1A3").IsUnique();

            entity.Property(e => e.MedicineTypeId).HasColumnName("MedicineTypeID");
            entity.Property(e => e.ApplicationType).HasMaxLength(100);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsManufactured).HasDefaultValue(false);
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAF62D15836");

            entity.HasIndex(e => new { e.OrderDate, e.RequiredDate, e.PickupDate }, "IX_Orders_Dates");

            entity.HasIndex(e => e.MedicineId, "IX_Orders_Medicine");

            entity.HasIndex(e => e.PatientId, "IX_Orders_Patient");

            entity.HasIndex(e => e.RequiredDate, "IX_Orders_RequiredDate").HasFilter("([OrderStatus]<>'PickedUp')");

            entity.HasIndex(e => e.OrderStatus, "IX_Orders_Status");

            entity.HasIndex(e => e.OrderNumber, "UQ__Orders__CAC5E743CFE75736").IsUnique();

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.AssignedEmployeeId).HasColumnName("AssignedEmployeeID");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsAllComponentsAvailable).HasDefaultValue(true);
            entity.Property(e => e.MedicineId).HasColumnName("MedicineID");
            entity.Property(e => e.MissingComponentsNote).HasMaxLength(1000);
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.OrderDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.OrderNumber).HasMaxLength(50);
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(50)
                .HasDefaultValue("New");
            entity.Property(e => e.PaidAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PatientAddress).HasMaxLength(500);
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.PatientNotificationSent).HasDefaultValue(false);
            entity.Property(e => e.PatientPhone).HasMaxLength(20);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Unpaid");
            entity.Property(e => e.PrescriptionId).HasColumnName("PrescriptionID");
            entity.Property(e => e.ProductionEmployeeId).HasColumnName("ProductionEmployeeID");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.AssignedEmployee).WithMany(p => p.OrderAssignedEmployees)
                .HasForeignKey(d => d.AssignedEmployeeId)
                .HasConstraintName("FK_Orders_AssignedEmployee");

            entity.HasOne(d => d.Medicine).WithMany(p => p.Orders)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Medicine");

            entity.HasOne(d => d.Patient).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Patient");

            entity.HasOne(d => d.Prescription).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PrescriptionId)
                .HasConstraintName("FK_Orders_Prescription");

            entity.HasOne(d => d.ProductionEmployee).WithMany(p => p.OrderProductionEmployees)
                .HasForeignKey(d => d.ProductionEmployeeId)
                .HasConstraintName("FK_Orders_ProductionEmployee");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId).HasName("PK__Patients__970EC34634D96836");

            entity.HasIndex(e => e.LastName, "IX_Patients_LastName");

            entity.HasIndex(e => e.Phone, "IX_Patients_Phone");

            entity.HasIndex(e => e.TotalOrdersCount, "IX_Patients_TotalOrders").IsDescending();

            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Age).HasComputedColumnSql("(datediff(year,[BirthDate],getdate()))", false);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.MedicalPolicyNumber).HasMaxLength(50);
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.PassportNumber).HasMaxLength(10);
            entity.Property(e => e.PassportSeries).HasMaxLength(10);
            entity.Property(e => e.Patronymic).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.RegistrationDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TotalOrdersCount).HasDefaultValue(0);
        });

        modelBuilder.Entity<PreparationTechnology>(entity =>
        {
            entity.HasKey(e => e.TechnologyId).HasName("PK__Preparat__705701782CEFD26E");

            entity.HasIndex(e => e.TechnologyCode, "UQ__Preparat__4D831950DE87442F").IsUnique();

            entity.Property(e => e.TechnologyId).HasColumnName("TechnologyID");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MedicineName).HasMaxLength(200);
            entity.Property(e => e.MedicineTypeId).HasColumnName("MedicineTypeID");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TechnologyCode).HasMaxLength(50);

            entity.HasOne(d => d.MedicineType).WithMany(p => p.PreparationTechnologies)
                .HasForeignKey(d => d.MedicineTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PreparationTechnologies_MedicineType");
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.PrescriptionId).HasName("PK__Prescrip__40130812DF135178");

            entity.HasIndex(e => e.IssueDate, "IX_Prescriptions_Date");

            entity.HasIndex(e => e.DoctorId, "IX_Prescriptions_Doctor");

            entity.HasIndex(e => e.IsFilled, "IX_Prescriptions_Filled");

            entity.HasIndex(e => e.PatientId, "IX_Prescriptions_Patient");

            entity.HasIndex(e => e.PrescriptionNumber, "UQ__Prescrip__05621DD4584A2CC9").IsUnique();

            entity.Property(e => e.PrescriptionId).HasColumnName("PrescriptionID");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Diagnosis).HasMaxLength(500);
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.DoctorSignature).HasDefaultValue(false);
            entity.Property(e => e.DoctorStamp).HasDefaultValue(false);
            entity.Property(e => e.Dosage).HasMaxLength(200);
            entity.Property(e => e.IsFilled).HasDefaultValue(false);
            entity.Property(e => e.IssueDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MedicineId).HasColumnName("MedicineID");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.PrescriptionNumber).HasMaxLength(50);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.UsageInstructions).HasMaxLength(500);

            entity.HasOne(d => d.Doctor).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prescriptions_Doctor");

            entity.HasOne(d => d.Medicine).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.MedicineId)
                .HasConstraintName("FK_Prescriptions_Medicine");

            entity.HasOne(d => d.Patient).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prescriptions_Patient");
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.MovementId).HasName("PK__StockMov__D1822466A4288029");

            entity.HasIndex(e => e.MovementDate, "IX_StockMovements_Date");

            entity.HasIndex(e => new { e.ItemType, e.ItemId }, "IX_StockMovements_Item");

            entity.HasIndex(e => e.MovementType, "IX_StockMovements_Type");

            entity.Property(e => e.MovementId).HasColumnName("MovementID");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DocumentNumber).HasMaxLength(50);
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.ItemType).HasMaxLength(20);
            entity.Property(e => e.MovementDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MovementType).HasMaxLength(50);
            entity.Property(e => e.NewStock).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.PerformedByEmployeeId).HasColumnName("PerformedByEmployeeID");
            entity.Property(e => e.PreviousStock).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.RelatedInventoryId).HasColumnName("RelatedInventoryID");
            entity.Property(e => e.RelatedOrderId).HasColumnName("RelatedOrderID");
            entity.Property(e => e.RelatedRequestId).HasColumnName("RelatedRequestID");
            entity.Property(e => e.UnitId).HasColumnName("UnitID");

            entity.HasOne(d => d.PerformedByEmployee).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.PerformedByEmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockMovements_Employee");

            entity.HasOne(d => d.RelatedInventory).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.RelatedInventoryId)
                .HasConstraintName("FK_StockMovements_Inventory");

            entity.HasOne(d => d.RelatedOrder).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.RelatedOrderId)
                .HasConstraintName("FK_StockMovements_Order");

            entity.HasOne(d => d.RelatedRequest).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.RelatedRequestId)
                .HasConstraintName("FK_StockMovements_Request");

            entity.HasOne(d => d.Unit).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockMovements_Unit");
        });

        modelBuilder.Entity<UnitsOfMeasure>(entity =>
        {
            entity.HasKey(e => e.UnitId).HasName("PK__UnitsOfM__44F5EC957491ACEC");

            entity.ToTable("UnitsOfMeasure");

            entity.HasIndex(e => e.UnitName, "UQ__UnitsOfM__B5EE6678F0136E88").IsUnique();

            entity.Property(e => e.UnitId).HasColumnName("UnitID");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UnitName).HasMaxLength(50);
            entity.Property(e => e.UnitSymbol).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

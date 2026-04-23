using Microsoft.EntityFrameworkCore;
using PharmacyClient.Models;

namespace PharmacyClient.Data
{
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
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Используем строку подключения из текущей сессии пользователя, если она есть
                var connectionString = App.CurrentUserSession?.ConnectionString;
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    // Если сессия не установлена, используем строку по умолчанию
                    connectionString = "Server=localhost;Database=PharmacyDB;Trusted_Connection=True;TrustServerCertificate=True;";
                }
                
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure all entities from the shared models
            modelBuilder.ConfigureModel();
        }
    }
}

using System;
using System.Collections.Generic;

namespace PharmacyClient.Models;

public partial class Medicine
{
    public int MedicineId { get; set; }

    public string MedicineName { get; set; } = null!;

    public int MedicineTypeId { get; set; }

    public int? CategoryId { get; set; }

    public int? TechnologyId { get; set; }

    public decimal CriticalNorm { get; set; }

    public decimal? CurrentStock { get; set; }

    public int UnitId { get; set; }

    public int? ShelfLifeDays { get; set; }

    public decimal? ManufacturingCost { get; set; }

    public decimal SalePrice { get; set; }

    public bool? RequiresPrescription { get; set; }

    public bool? IsReadyMade { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual MedicineCategory? Category { get; set; }

    public virtual ICollection<MedicineComposition> MedicineCompositions { get; set; } = new List<MedicineComposition>();

    public virtual MedicineType MedicineType { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public virtual PreparationTechnology? Technology { get; set; }

    public virtual UnitsOfMeasure Unit { get; set; } = null!;
}

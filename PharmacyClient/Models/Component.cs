using System;
using System.Collections.Generic;

namespace PharmacyClient.Models;

public partial class Component
{
    public int ComponentId { get; set; }

    public string ComponentName { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int UnitId { get; set; }

    public decimal CriticalNorm { get; set; }

    public decimal? CurrentStock { get; set; }

    public decimal PurchasePrice { get; set; }

    public int? ShelfLifeDays { get; set; }

    public string? Supplier { get; set; }

    public string? StorageConditions { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual MedicineCategory? Category { get; set; }

    public virtual ICollection<ComponentRequest> ComponentRequests { get; set; } = new List<ComponentRequest>();

    public virtual ICollection<MedicineComposition> MedicineCompositions { get; set; } = new List<MedicineComposition>();

    public virtual UnitsOfMeasure Unit { get; set; } = null!;
}

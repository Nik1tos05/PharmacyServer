using System;
using System.Collections.Generic;

namespace PharmacyClient.Models;

public partial class InventoryDetail
{
    public int DetailId { get; set; }

    public int InventoryId { get; set; }

    public string ItemType { get; set; } = null!;

    public int ItemId { get; set; }

    public decimal ExpectedQuantity { get; set; }

    public decimal ActualQuantity { get; set; }

    public decimal? Difference { get; set; }

    public int UnitId { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    public bool? IsExpired { get; set; }

    public bool? IsBelowCriticalNorm { get; set; }

    public string? Condition { get; set; }

    public string? Notes { get; set; }

    public int? CheckedByEmployeeId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Employee? CheckedByEmployee { get; set; }

    public virtual InventoryCheck Inventory { get; set; } = null!;

    public virtual UnitsOfMeasure Unit { get; set; } = null!;
}

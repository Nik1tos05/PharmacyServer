using System;
using System.Collections.Generic;

namespace PharmacyClient.Models;

public partial class UnitsOfMeasure
{
    public int UnitId { get; set; }

    public string UnitName { get; set; } = null!;

    public string? UnitSymbol { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<Component> Components { get; set; } = new List<Component>();

    public virtual ICollection<InventoryDetail> InventoryDetails { get; set; } = new List<InventoryDetail>();

    public virtual ICollection<MedicineComposition> MedicineCompositions { get; set; } = new List<MedicineComposition>();

    public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

using System;
using System.Collections.Generic;

namespace PharmacyClient.Models;

public partial class MedicineComposition
{
    public int CompositionId { get; set; }

    public int MedicineId { get; set; }

    public int ComponentId { get; set; }

    public decimal Quantity { get; set; }

    public int UnitId { get; set; }

    public int? SequenceNumber { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Component Component { get; set; } = null!;

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual UnitsOfMeasure Unit { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace PharmacyServer.Models;

public partial class InventoryCheck
{
    public int InventoryId { get; set; }

    public string InventoryNumber { get; set; } = null!;

    public DateTime CheckDate { get; set; }

    public DateOnly? ScheduledDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public string Status { get; set; } = null!;

    public int ConductedByEmployeeId { get; set; }

    public int? CheckedByEmployeeId { get; set; }

    public int? TotalItemsChecked { get; set; }

    public int? DiscrepanciesFound { get; set; }

    public int? ExpiredItemsCount { get; set; }

    public int? CriticalNormViolations { get; set; }

    public decimal? ShortageValue { get; set; }

    public decimal? SurplusValue { get; set; }

    public string? Notes { get; set; }

    public bool? ReportGenerated { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual Employee? CheckedByEmployee { get; set; }

    public virtual Employee ConductedByEmployee { get; set; } = null!;

    public virtual ICollection<InventoryDetail> InventoryDetails { get; set; } = new List<InventoryDetail>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

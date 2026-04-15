using System;
using System.Collections.Generic;

namespace PharmacyServer.Models;

public partial class StockMovement
{
    public int MovementId { get; set; }

    public DateTime MovementDate { get; set; }

    public string ItemType { get; set; } = null!;

    public int ItemId { get; set; }

    public string MovementType { get; set; } = null!;

    public decimal Quantity { get; set; }

    public int UnitId { get; set; }

    public decimal? PreviousStock { get; set; }

    public decimal? NewStock { get; set; }

    public int? RelatedOrderId { get; set; }

    public int? RelatedInventoryId { get; set; }

    public int? RelatedRequestId { get; set; }

    public int PerformedByEmployeeId { get; set; }

    public string? Reason { get; set; }

    public string? DocumentNumber { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Employee PerformedByEmployee { get; set; } = null!;

    public virtual InventoryCheck? RelatedInventory { get; set; }

    public virtual Order? RelatedOrder { get; set; }

    public virtual ComponentRequest? RelatedRequest { get; set; }

    public virtual UnitsOfMeasure Unit { get; set; } = null!;
}

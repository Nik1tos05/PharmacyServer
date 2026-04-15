using System;
using System.Collections.Generic;

namespace PharmacyServer.Models;

public partial class ComponentRequest
{
    public int RequestId { get; set; }

    public string RequestNumber { get; set; } = null!;

    public int ComponentId { get; set; }

    public decimal RequestedQuantity { get; set; }

    public DateTime RequestDate { get; set; }

    public DateTime? RequiredDate { get; set; }

    public DateTime? ReceivedDate { get; set; }

    public string RequestStatus { get; set; } = null!;

    public int? OrderId { get; set; }

    public string? Supplier { get; set; }

    public decimal? ExpectedPrice { get; set; }

    public decimal? ActualPrice { get; set; }

    public int RequestedByEmployeeId { get; set; }

    public int? ApprovedByEmployeeId { get; set; }

    public string? DeliveryNotes { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual Employee? ApprovedByEmployee { get; set; }

    public virtual Component Component { get; set; } = null!;

    public virtual Order? Order { get; set; }

    public virtual Employee RequestedByEmployee { get; set; } = null!;

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

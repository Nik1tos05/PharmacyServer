using System;
using System.Collections.Generic;

namespace PharmacyServer.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public int? PrescriptionId { get; set; }

    public int PatientId { get; set; }

    public int MedicineId { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? RequiredDate { get; set; }

    public DateTime? ReadyDate { get; set; }

    public DateTime? PickupDate { get; set; }

    public string OrderStatus { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal? PaidAmount { get; set; }

    public string? PaymentStatus { get; set; }

    public int? AssignedEmployeeId { get; set; }

    public int? ProductionEmployeeId { get; set; }

    public bool? IsAllComponentsAvailable { get; set; }

    public string? MissingComponentsNote { get; set; }

    public bool? PatientNotificationSent { get; set; }

    public string? PatientPhone { get; set; }

    public string? PatientAddress { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual Employee? AssignedEmployee { get; set; }

    public virtual ICollection<ComponentRequest> ComponentRequests { get; set; } = new List<ComponentRequest>();

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual Prescription? Prescription { get; set; }

    public virtual Employee? ProductionEmployee { get; set; }

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

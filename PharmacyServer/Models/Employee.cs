using System;
using System.Collections.Generic;

namespace PharmacyServer.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? Patronymic { get; set; }

    public string Position { get; set; } = null!;

    public string? Department { get; set; }

    public DateOnly HireDate { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? PassportSeries { get; set; }

    public string? PassportNumber { get; set; }

    public decimal? Salary { get; set; }

    public bool? IsManager { get; set; }

    public bool? CanSignDocuments { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual ICollection<ComponentRequest> ComponentRequestApprovedByEmployees { get; set; } = new List<ComponentRequest>();

    public virtual ICollection<ComponentRequest> ComponentRequestRequestedByEmployees { get; set; } = new List<ComponentRequest>();

    public virtual ICollection<InventoryCheck> InventoryCheckCheckedByEmployees { get; set; } = new List<InventoryCheck>();

    public virtual ICollection<InventoryCheck> InventoryCheckConductedByEmployees { get; set; } = new List<InventoryCheck>();

    public virtual ICollection<InventoryDetail> InventoryDetails { get; set; } = new List<InventoryDetail>();

    public virtual ICollection<Order> OrderAssignedEmployees { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderProductionEmployees { get; set; } = new List<Order>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

using System;
using System.Collections.Generic;

namespace PharmacyServer.Models;

public partial class MedicineCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int? ParentCategoryId { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Component> Components { get; set; } = new List<Component>();

    public virtual ICollection<MedicineCategory> InverseParentCategory { get; set; } = new List<MedicineCategory>();

    public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();

    public virtual MedicineCategory? ParentCategory { get; set; }
}

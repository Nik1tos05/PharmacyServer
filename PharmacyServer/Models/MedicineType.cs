using System;
using System.Collections.Generic;

namespace PharmacyServer.Models;

public partial class MedicineType
{
    public int MedicineTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public string? ApplicationType { get; set; }

    public bool? IsManufactured { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();

    public virtual ICollection<PreparationTechnology> PreparationTechnologies { get; set; } = new List<PreparationTechnology>();
}

using System;
using System.Collections.Generic;

namespace PharmacyClient.Models;

public partial class PreparationTechnology
{
    public int TechnologyId { get; set; }

    public string TechnologyCode { get; set; } = null!;

    public string MedicineName { get; set; } = null!;

    public string PreparationMethod { get; set; } = null!;

    public int PreparationTimeMinutes { get; set; }

    public int MedicineTypeId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual MedicineType MedicineType { get; set; } = null!;

    public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}

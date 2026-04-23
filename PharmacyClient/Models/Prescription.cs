using System;
using System.Collections.Generic;

namespace PharmacyClient.Models;

public partial class Prescription
{
    public int PrescriptionId { get; set; }

    public string PrescriptionNumber { get; set; } = null!;

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public DateTime IssueDate { get; set; }

    public DateOnly? ValidUntil { get; set; }

    public string? Diagnosis { get; set; }

    public int? MedicineId { get; set; }

    public decimal Quantity { get; set; }

    public string? Dosage { get; set; }

    public string? UsageInstructions { get; set; }

    public bool? IsFilled { get; set; }

    public DateTime? FilledDate { get; set; }

    public string? Notes { get; set; }

    public bool? DoctorSignature { get; set; }

    public bool? DoctorStamp { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Medicine? Medicine { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Patient Patient { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace PharmacyServer.Models;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? Patronymic { get; set; }

    public string? Specialization { get; set; }

    public string? LicenseNumber { get; set; }

    public string? ClinicName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public byte[]? SignatureImage { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}

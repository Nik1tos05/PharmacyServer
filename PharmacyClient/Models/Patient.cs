using System;
using System.Collections.Generic;

namespace PharmacyClient.Models;

public partial class Patient
{
    public int PatientId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? Patronymic { get; set; }

    public DateOnly? BirthDate { get; set; }

    public int? Age { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? PassportSeries { get; set; }

    public string? PassportNumber { get; set; }

    public string? MedicalPolicyNumber { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public DateTime? LastVisitDate { get; set; }

    public int? TotalOrdersCount { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}

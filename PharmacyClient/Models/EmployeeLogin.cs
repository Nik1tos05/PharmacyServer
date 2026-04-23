using System;
using System.Collections.Generic;

namespace PharmacyClient.Models;

public partial class EmployeeLogin
{
    public string LoginName { get; set; } = null!;

    public int? EmployeeId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Employee? Employee { get; set; }
}

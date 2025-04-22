using System;
using System.Collections.Generic;

namespace TaskManagementAPI.Models;

public partial class Address
{
    public Guid Id { get; set; }

    public string? StreetAddress { get; set; }

    public string City { get; set; } = null!;

    public string? ZipCode { get; set; }
}

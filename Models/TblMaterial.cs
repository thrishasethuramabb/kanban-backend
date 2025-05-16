using System;
using System.Collections.Generic;

namespace kanbanBackend.Models;

public partial class TblMaterial
{
    public int Id { get; set; }

    public string? StrName { get; set; }

    public string? StrDescription { get; set; }
    public string? ImagePath { get; set; }
}

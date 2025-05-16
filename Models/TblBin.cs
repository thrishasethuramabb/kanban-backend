using System;
using System.Collections.Generic;

namespace kanbanBackend.Models;

public partial class TblBin
{
    public int Id { get; set; }

    public string? StrName { get; set; }

    public int? IntSize { get; set; }
}

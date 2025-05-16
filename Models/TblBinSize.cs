using System;
using System.Collections.Generic;

namespace kanbanBackend.Models;

public partial class TblBinSize
{
    public int Id { get; set; }

    public string? StrShortName { get; set; }

    public string? StrName { get; set; }
}

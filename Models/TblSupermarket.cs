using System;
using System.Collections.Generic;

namespace kanbanBackend.Models;

public partial class TblSupermarket
{
    public int Id { get; set; }

    public int IntMaterial { get; set; }

    public int? IntBin { get; set; }

    public int? IntStorageType { get; set; }

    public int? IntQuantity { get; set; }

    public int? IntRotation { get; set; }
}

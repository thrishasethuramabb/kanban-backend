using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kanbanBackend.Models;

[Table("tblEmployee")]
public class Employee
{
    [Key]
    public long IntEmployeeId { get; set; }

    public string StrEmployeeFirstName { get; set; }

    public string StrEmployeeLastName { get; set; }

    public string StrUsername { get; set; }

    public string StrPassword { get; set; }

    public bool BitIsActive { get; set; }

    public string StrRole { get; set; }

    public long? IntAddedBy { get; set; }
}

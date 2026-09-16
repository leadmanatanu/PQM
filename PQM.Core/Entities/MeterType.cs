using PQM.Core.Entities;
using System.ComponentModel.DataAnnotations;

public class MeterType
{
    [Key]
    public int Id { get; set; }
    public string? Name { get; set; } 
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

}
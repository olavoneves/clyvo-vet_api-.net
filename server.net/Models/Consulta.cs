using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.net.Models;

[Table("CONSULTAS")]
public class Consulta
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [Column("DATA")]
    public DateTime Data { get; set; }

    [Column("DESCRICAO")]
    public string? Descricao { get; set; }

    [Column("DIAGNOSTICO")]
    public string? Diagnostico { get; set; }

    [Required]
    [Column("VETERINARIO_NOME")]
    public required string VeterinarioNome { get; set; }

    [Column("PET_ID")]
    public int PetId { get; set; }

    public Pet Pet { get; set; } = null!;
}

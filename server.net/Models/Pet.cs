using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.net.Models;

[Table("PETS")]
public class Pet
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [Column("NOME")]
    public required string Nome { get; set; }

    [Required]
    [Column("ESPECIE")]
    public required string Especie { get; set; }

    [Column("RACA")]
    public string? Raca { get; set; }

    [Column("DATA_NASCIMENTO")]
    public DateTime? DataNascimento { get; set; }

    [Column("TUTOR_ID")]
    public int TutorId { get; set; }

    public Tutor Tutor { get; set; } = null!;

    public List<Consulta> Consultas { get; set; } = [];
}

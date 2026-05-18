using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.net.Models;

[Table("TUTORES")]
public class Tutor
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [Column("NOME")]
    public required string Nome { get; set; }

    [Required]
    [EmailAddress]
    [Column("EMAIL")]
    public required string Email { get; set; }

    [Column("TELEFONE")]
    public string? Telefone { get; set; }

    [Column("CPF")]
    public string? Cpf { get; set; }

    public List<Pet> Pets { get; set; } = [];
}

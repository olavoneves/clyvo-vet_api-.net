using Microsoft.EntityFrameworkCore;
using server.net.Models;

namespace server.net.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tutor> Tutores => Set<Tutor>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Consulta> Consultas => Set<Consulta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tutor>()
            .HasMany(t => t.Pets)
            .WithOne(p => p.Tutor)
            .HasForeignKey(p => p.TutorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Pet>()
            .HasMany(p => p.Consultas)
            .WithOne(c => c.Pet)
            .HasForeignKey(c => c.PetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

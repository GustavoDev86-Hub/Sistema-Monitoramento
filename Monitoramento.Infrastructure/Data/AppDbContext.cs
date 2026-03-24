using Microsoft.EntityFrameworkCore;
using Monitoramento.Domain.Entities;

namespace Monitoramento.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Ativo> Ativos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ativo>(entity =>
        {
            entity.ToTable("tb_ativos");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Url)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("ds_url");

            entity.Property(a => a.EstaOnline)
                .HasDefaultValue(false);

            entity.Property(a => a.UltimaVerificacao)
                .HasColumnType("timestamp with time zone");
        });
    }
}
using Microsoft.EntityFrameworkCore;
using SobraBoa.Domain.Entities;

namespace SobraBoa.Infrastructure.Persistence;

public class SobraBoaContext(DbContextOptions<SobraBoaContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Doacao> Doacoes => Set<Doacao>();
    public DbSet<ItemDoacao> ItensDoacao => Set<ItemDoacao>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Comprovante> Comprovantes => Set<Comprovante>();
    public DbSet<HistoricoStatus> Historico => Set<HistoricoStatus>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Usuario>(e =>
        {
            e.ToTable("SB_USUARIOS");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            e.Property(x => x.Email).HasMaxLength(180).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.SenhaHash).HasMaxLength(300).IsRequired();
            e.Property(x => x.NomeOrganizacao).HasMaxLength(150);
            e.Property(x => x.Papel).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Aprovacao).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.PossuiRefrigeracao).HasConversion<int>().HasColumnType("NUMBER(1)");
        });

        b.Entity<Doacao>(e =>
        {
            e.ToTable("SB_DOACOES");
            e.Property(x => x.Titulo).HasMaxLength(150).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RequerRefrigeracao).HasConversion<int>().HasColumnType("NUMBER(1)");
            e.HasMany(x => x.Itens).WithOne().HasForeignKey(i => i.DoacaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.DoadorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.Status, x.JanelaFim });
            e.Ignore(x => x.PesoTotalKg);
        });

        b.Entity<ItemDoacao>(e =>
        {
            e.ToTable("SB_ITENS_DOACAO");
            e.Property(x => x.Unidade).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.Quantidade).HasPrecision(10, 2);
            e.Property(x => x.PesoEstimadoKg).HasPrecision(10, 2);
        });

        b.Entity<Reserva>(e =>
        {
            e.ToTable("SB_RESERVAS");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CodigoRetirada).HasMaxLength(10);
            e.HasOne<Doacao>().WithMany().HasForeignKey(x => x.DoacaoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.ReceptorId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Comprovante>(e =>
        {
            e.ToTable("SB_COMPROVANTES");
            e.HasIndex(x => x.ReservaId).IsUnique(); // 1 comprovante por reserva
            e.HasIndex(x => x.CodigoVerificacao).IsUnique();
            e.Property(x => x.PesoTotalKg).HasPrecision(10, 2);
        });

        b.Entity<HistoricoStatus>(e =>
        {
            e.ToTable("SB_HISTORICO");
            e.Property(x => x.StatusAnterior).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.StatusNovo).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.DoacaoId);
        });

        b.Entity<IdempotencyRecord>(e =>
        {
            e.ToTable("SB_IDEMPOTENCIA");
            e.Property(x => x.Chave).HasMaxLength(100).IsRequired();
            e.Property(x => x.Escopo).HasMaxLength(300).IsRequired();
            e.HasIndex(x => new { x.Chave, x.Escopo }).IsUnique(); // a garantia de "uma vez só"
            e.Property(x => x.CorpoResposta).HasColumnType("CLOB");
        });
    }
}
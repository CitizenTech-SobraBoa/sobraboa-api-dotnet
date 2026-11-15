namespace SobraBoa.Domain.Entities;

public class IdempotencyRecord
{
    public int Id { get; set; }
    public string Chave { get; set; } = default!;
    public string Escopo { get; set; } = default!;
    public int StatusCode { get; set; }
    public string? CorpoResposta { get; set; }
    public DateTime CriadoEm { get; set; }
}
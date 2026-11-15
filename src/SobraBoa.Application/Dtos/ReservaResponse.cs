namespace SobraBoa.Application.Dtos;

public record ReservaResponse(int Id, int DoacaoId, string Status, string? CodigoRetirada, DateTime ExpiraEm);

public record ComprovanteResponse(
    string CodigoVerificacao,
    decimal PesoTotalKg,
    DateTime EmitidoEm,
    int DoacaoId,
    int ReservaId);

public record MetricasResponse(
    IReadOnlyDictionary<string, int> DoacoesPorStatus,
    decimal KgDoados,
    int DoadoresAprovados,
    int ReceptoresAprovados);
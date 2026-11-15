namespace SobraBoa.Application.Dtos;

public record DoacaoResponse(
    int Id,
    string Titulo,
    string? Descricao,
    string Status,
    DateTime JanelaInicio,
    DateTime JanelaFim,
    bool RequerRefrigeracao,
    string? InstrucoesRetirada,
    decimal PesoTotalKg,
    double? DistanciaKm,
    List<ItemResponse> Itens);

public record ItemResponse(
    int Id,
    string Descricao,
    decimal Quantidade,
    string Unidade,
    decimal PesoEstimadoKg,
    DateOnly? DataValidade);
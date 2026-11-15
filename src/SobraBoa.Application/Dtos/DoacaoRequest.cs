using System.ComponentModel.DataAnnotations;

namespace SobraBoa.Application.Dtos;

public record DoacaoRequest(
    [Required, StringLength(150)] string Titulo,
    string? Descricao,
    DateTime JanelaInicio,
    DateTime JanelaFim,
    bool RequerRefrigeracao,
    string? InstrucoesRetirada,
    [Required, MinLength(1, ErrorMessage = "Informe ao menos um item")]
    List<ItemRequest> Itens);
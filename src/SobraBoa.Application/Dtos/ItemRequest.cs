using System.ComponentModel.DataAnnotations;
using SobraBoa.Domain.Enums;

namespace SobraBoa.Application.Dtos;

public record ItemRequest(
    [Required] string Descricao,
    [Range(0.01, 100000)] decimal Quantidade,
    UnidadeMedida Unidade,
    [Range(0, 100000)] decimal PesoEstimadoKg,
    DateOnly? DataValidade);
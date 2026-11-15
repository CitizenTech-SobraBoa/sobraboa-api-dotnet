using System.ComponentModel.DataAnnotations;

namespace SobraBoa.Application.Dtos;

public record CodigoRequest([Required] string Codigo);

public record CancelarRequest(string? Motivo);
    
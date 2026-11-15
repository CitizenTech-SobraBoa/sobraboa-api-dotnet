using System.ComponentModel.DataAnnotations;

namespace SobraBoa.Application.Dtos;

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Senha);

public record LoginResponse(
    string Token,
    string Tipo,
    DateTime ExpiraEm,
    int UsuarioId,
    string Nome,
    string Papel,
    string Aprovacao);
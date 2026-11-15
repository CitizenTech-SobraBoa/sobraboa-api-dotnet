using System.ComponentModel.DataAnnotations;
using SobraBoa.Domain.Enums;

namespace SobraBoa.Application.Dtos;

public record RegistroRequest(
    [Required, StringLength(120)] string Nome,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8, ErrorMessage = "A senha deve ter ao menos 8 caracteres")]
    string Senha,
    [Required] Papel Papel, // Doador ou Receptor
    [Required, StringLength(150)] string NomeOrganizacao,
    string? Cnpj,
    string? Cep,
    [Range(-90, 90)] double Latitude,
    [Range(-180, 180)] double Longitude,
    bool PossuiRefrigeracao,
    [Range(typeof(bool), "true", "true", ErrorMessage = "É necessário aceitar os termos")]
    bool AceitouTermos);
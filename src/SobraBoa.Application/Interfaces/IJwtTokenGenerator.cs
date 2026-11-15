using SobraBoa.Domain.Entities;

namespace SobraBoa.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiraEm) Gerar(Usuario u);
}
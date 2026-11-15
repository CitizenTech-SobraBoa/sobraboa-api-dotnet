using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Entities;

namespace SobraBoa.Infrastructure.Security;

public class JwtTokenGenerator(IConfiguration cfg) : IJwtTokenGenerator
{
    public (string Token, DateTime ExpiraEm) Gerar(Usuario u)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
        var expira = DateTime.UtcNow.AddMinutes(int.Parse(cfg["Jwt:ExpiresInMinutes"] ?? "60"));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, u.Email),
            new Claim("name", u.Nome),
            new Claim("role", u.Papel.ToString()) // Doador | Receptor | Admin
        };
        var jwt = new JwtSecurityToken(cfg["Jwt:Issuer"], cfg["Jwt:Audience"], claims,
            expires: expira, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expira);
    }
}
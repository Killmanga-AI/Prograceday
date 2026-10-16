using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Models;
namespace RaceDay.Api.Services;
public interface ITokenService { string Create(User user, string role); }
public sealed class TokenService(IConfiguration config) : ITokenService {
 public string Create(User user,string role) { var key=config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key must be configured."); if(key.Length<32) throw new InvalidOperationException("Jwt:Key must contain at least 32 characters."); var claims=new[]{new Claim(ClaimTypes.NameIdentifier,user.UserId.ToString()),new Claim(ClaimTypes.Name,user.FullName),new Claim(ClaimTypes.Email,user.Email),new Claim(ClaimTypes.Role,role)}; var creds=new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),SecurityAlgorithms.HmacSha256); var token=new JwtSecurityToken(config["Jwt:Issuer"]??"RaceDay",config["Jwt:Audience"]??"RaceDayClients",claims,expires:DateTime.UtcNow.AddHours(2),signingCredentials:creds); return new JwtSecurityTokenHandler().WriteToken(token); }
}

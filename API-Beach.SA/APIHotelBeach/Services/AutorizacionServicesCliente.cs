using APIHotelBeach.Models;
using APIHotelBeach.Models.Custom;
using APIHotelBeach.Context;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIHotelBeach.Services
{
    public class AutorizacionServicesCliente : IAutorizacionServicesCliente
    {

        private readonly IConfiguration _configuration;
        private readonly DbContextHotel _context;

        public AutorizacionServicesCliente(IConfiguration configuration, DbContextHotel context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<AutorizacionResponse> DevolverToken(Cliente autorizacion)
        {

            var temp = await _context.Clientes.FirstOrDefaultAsync(u => u.Email.Equals(autorizacion.Email) && u.Password.Equals(autorizacion.Password));

            if (temp == null || temp.TipoUsuario != 3 || temp.Estado != 'A')
            {
                return null;
            }

            string tokenCreado = GenerarToken($"customer:{temp.Cedula}", "Customer");

            return new AutorizacionResponse() { Token = tokenCreado, Resultado = true, Msj = "Ok" };
        }

        private string GenerarToken(string userId, string role)
        {
            var key = _configuration.GetValue<string>("JwtSettings:Key");
            var keyBytes = Encoding.ASCII.GetBytes(key);

            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
            claims.AddClaim(new Claim(ClaimTypes.Role, role));

            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Expires = DateTime.UtcNow.AddMinutes(10),
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}

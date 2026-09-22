using APIHotelBeach.Context;
using APIHotelBeach.Models;
using APIHotelBeach.Models.Custom;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace APIHotelBeach.Services
{
    public class AutorizacionServicesEmpleado : IAutorizacionServicesEmpleado
    {
        private readonly IConfiguration _configuration;
        private readonly DbContextHotel _context;

        public AutorizacionServicesEmpleado(IConfiguration configuration, DbContextHotel context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<AutorizacionResponse> DevolverTokenEmpleado(Empleado autorizacion)
        {
            if (autorizacion == null || string.IsNullOrWhiteSpace(autorizacion.Email) || string.IsNullOrEmpty(autorizacion.Password))
            {
                return null;
            }

            string normalizedEmail = autorizacion.Email.Trim().ToUpperInvariant();

            var employee = await _context.Empleados.AsNoTracking().FirstOrDefaultAsync(e => e.Email.Trim().ToUpper() == normalizedEmail && e.Estado == 'A' && (e.TipoUsuario == 1 || e.TipoUsuario == 2));

            if (employee == null || !VerifyEmployeePassword(employee, autorizacion.Password))
            {
                return null;
            }

            string role = employee.TipoUsuario == 1 ? "Admin" : "Employee";
            string token = GenerarToken($"employee:{employee.ID}", role);

            return new AutorizacionResponse
            {
                Token = token,
                Resultado = true,
                Msj = "Authentication successful."
            };
        }

        private bool VerifyEmployeePassword(Empleado employee, string password)
        {
            if (string.IsNullOrEmpty(employee.Password) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            const string prefix = "IdentityV3:";

            if (employee.Password.StartsWith(prefix, StringComparison.Ordinal))
            {
                try
                {
                    var hasher = new PasswordHasher<Empleado>();
                    string storedHash = employee.Password.Substring(prefix.Length);
                    var result = hasher.VerifyHashedPassword(employee, storedHash, password);

                    return result != PasswordVerificationResult.Failed;
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            byte[] storedPassword = Encoding.UTF8.GetBytes(employee.Password);
            byte[] suppliedPassword = Encoding.UTF8.GetBytes(password);

            return CryptographicOperations.FixedTimeEquals(storedPassword, suppliedPassword);
        }

        private string GenerarToken(string userId, string role)
        {
            string key = _configuration.GetValue<string>("JwtSettings:Key");

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("The JWT signing key is not configured.");
            }

            byte[] keyBytes = Encoding.ASCII.GetBytes(key);

            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.UtcNow.AddMinutes(10),
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}

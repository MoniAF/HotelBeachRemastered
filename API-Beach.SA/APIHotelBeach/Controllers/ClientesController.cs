using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIHotelBeach.Models;
using APIHotelBeach.Context;
using iText.Commons.Actions.Contexts;
using APIHotelBeach.Services;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using static APIHotelBeach.Models.Cliente;
using Microsoft.AspNetCore.Authorization;
using APIHotelBeach.DTOs;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace APIHotelBeach.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class ClientesController : Controller
    {

        private readonly DbContextHotel _context;
        private readonly IAutorizacionServicesCliente autorizacionService;

        public ClientesController(DbContextHotel pContext, IAutorizacionServicesCliente autorizacionService)
        {
            _context = pContext;
            this.autorizacionService = autorizacionService;
        }

        //***   MÉTODOS  CRUD   ***

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("Listado")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var clientes = await _context.Clientes.AsNoTracking().OrderBy(c => c.NombreCompleto).ThenBy(c => c.Cedula).Select(c => new
                {
                    c.Cedula,
                    c.TipoCedula,
                    c.NombreCompleto,
                    c.Telefono,
                    c.Direccion,
                    c.Email,
                    c.FechaRegistro,
                    c.Estado
                }).ToListAsync();

                return Ok(clientes);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The customer list could not be loaded.");
            }
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("ClienteLogin")]
        public async Task<ActionResult<object>> DatosCliente()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId) || !userId.StartsWith("customer:"))
            {
                return Unauthorized();
            }

            string customerId = userId.Substring("customer:".Length);

            if (string.IsNullOrWhiteSpace(customerId))
            {
                return Unauthorized();
            }

            var customer = await _context.Clientes.Where(c => c.Cedula == customerId && c.Estado == 'A').Select(c => new { TipoUsuario = c.TipoUsuario, Cedula = c.Cedula, Restablecer = c.Restablecer }).FirstOrDefaultAsync();

            if (customer == null)
            {
                return Unauthorized();
            }

            return Ok(customer);
        }


        //Registrar cliente
        [AllowAnonymous]
        [HttpPost("CrearCuenta")]
        public async Task<IActionResult> CrearCuenta(CustomerRegisterRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { Message = "Customer information is required." });
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                string cedula = request.Cedula.Trim();
                string email = request.Email.Trim();
                string normalizedEmail = email.ToUpperInvariant();

                await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                bool existeCliente = await _context.Clientes.AnyAsync(c => c.Cedula == cedula || c.Email.Trim().ToUpper() == normalizedEmail);

                if (existeCliente)
                {
                    return Conflict(new { Message = "An account already exists with that identification number or email address." });
                }

                Cliente cliente = new Cliente();
                cliente.Cedula = cedula;
                cliente.TipoCedula = request.TipoCedula.Trim();
                cliente.NombreCompleto = request.NombreCompleto.Trim();
                cliente.Telefono = request.Telefono.Trim();
                cliente.Direccion = request.Direccion.Trim();
                cliente.Email = email;
                cliente.TipoUsuario = 3;
                cliente.Restablecer = 1;
                cliente.FechaRegistro = DateTime.Now;
                cliente.Estado = 'A';
                cliente.Password = HashCustomerPassword(cliente, request.Password);

                ClienteAuditoria auditoria = new ClienteAuditoria();
                auditoria.Accion = "AGREGADO";
                auditoria.FechaCambio = DateTime.Now;
                auditoria.Cedula = cliente.Cedula;
                auditoria.TipoCedula = cliente.TipoCedula;
                auditoria.NombreCompleto = cliente.NombreCompleto;
                auditoria.Telefono = cliente.Telefono;
                auditoria.Direccion = cliente.Direccion;
                auditoria.Email = cliente.Email;
                auditoria.Password = null;
                auditoria.TipoUsuario = cliente.TipoUsuario;
                auditoria.Restablecer = cliente.Restablecer;
                auditoria.FechaRegistro = cliente.FechaRegistro;
                auditoria.Estado = cliente.Estado;

                _context.Clientes.Add(cliente);
                _context.Clientes_Auditoria.Add(auditoria);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Account created successfully. You can now sign in." });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The account could not be created.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "An unexpected error occurred while creating the account.");
            }
        }

        //metodo para buscar por cedula
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("Buscar")]
        public async Task<IActionResult> GetClient(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
            {
                return BadRequest(new { Message = "Customer ID is required." });
            }

            cedula = cedula.Trim();

            var cliente = await _context.Clientes.AsNoTracking().Where(c => c.Cedula == cedula).Select(c => new
            {
                c.Cedula,
                c.TipoCedula,
                c.NombreCompleto,
                c.Telefono,
                c.Direccion,
                c.Email,
                c.FechaRegistro,
                c.Estado
            }).FirstOrDefaultAsync();

            if (cliente == null)
            {
                return NotFound(new { Message = "Customer not found." });
            }

            return Ok(cliente);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("BuscarCorreo")]
        public async Task<IActionResult> GetClientCorreo(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { Message = "The email address is required." });
            }

            string normalizedEmail = email.Trim().ToUpperInvariant();

            var customer = await _context.Clientes.AsNoTracking().Where(c => c.Email.Trim().ToUpper() == normalizedEmail).Select(c => new
            {
                c.Cedula,
                c.TipoCedula,
                c.NombreCompleto,
                c.Telefono,
                c.Direccion,
                c.Email,
                c.FechaRegistro,
                c.Estado
            }).FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { Message = "Customer not found." });
            }

            return Ok(customer);
        }

        //Modificar cliente
        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("Modificar")]
        public async Task<IActionResult> Modificar(CustomerEditRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { Message = "Customer information is required." });
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                string cedula = request.Cedula.Trim();
                string email = request.Email.Trim();

                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Cedula == cedula);

                if (cliente == null)
                {
                    return NotFound(new { Message = "Customer not found." });
                }

                string normalizedEmail = email.ToUpperInvariant();

                bool emailRegistrado = await _context.Clientes.AnyAsync(c => c.Cedula != cedula && c.Email.Trim().ToUpper() == normalizedEmail);

                if (emailRegistrado)
                {
                    return Conflict(new { Message = "That email address is already registered to another customer." });
                }

                ClienteAuditoria auditoria = new ClienteAuditoria();
                auditoria.Accion = "MODIFICADO";
                auditoria.FechaCambio = DateTime.Now;
                auditoria.Cedula = cliente.Cedula;
                auditoria.TipoCedula = cliente.TipoCedula;
                auditoria.NombreCompleto = cliente.NombreCompleto;
                auditoria.Telefono = cliente.Telefono;
                auditoria.Direccion = cliente.Direccion;
                auditoria.Email = cliente.Email;
                auditoria.Password = null;
                auditoria.TipoUsuario = cliente.TipoUsuario;
                auditoria.Restablecer = cliente.Restablecer;
                auditoria.FechaRegistro = cliente.FechaRegistro;
                auditoria.Estado = cliente.Estado;

                cliente.NombreCompleto = request.NombreCompleto.Trim();
                cliente.Telefono = request.Telefono.Trim();
                cliente.Direccion = request.Direccion.Trim();
                cliente.Email = email;

                _context.Clientes_Auditoria.Add(auditoria);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Customer updated successfully." });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The customer changes could not be saved.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "An unexpected error occurred while updating the customer.");
            }
        }

        //eliminar cliente
        [Authorize(Roles = "Admin,Employee")]
        [HttpDelete("EliminarCliente")]
        public async Task<IActionResult> Eliminar(string vCedula)
        {
            if (string.IsNullOrWhiteSpace(vCedula))
            {
                return BadRequest(new { Message = "Customer ID is required." });
            }

            try
            {
                string cedula = vCedula.Trim();

                await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Cedula == cedula);

                if (cliente == null)
                {
                    return NotFound(new { Message = "Customer not found." });
                }

                bool tieneReservaciones = await _context.Reservaciones.AnyAsync(r => r.CedulaCliente == cliente.Cedula);

                if (tieneReservaciones)
                {
                    return Conflict(new { Message = "This customer has reservations and cannot be deleted." });
                }

                ClienteAuditoria auditoria = new ClienteAuditoria();
                auditoria.Accion = "ELIMINADO";
                auditoria.FechaCambio = DateTime.Now;
                auditoria.Cedula = cliente.Cedula;
                auditoria.TipoCedula = cliente.TipoCedula;
                auditoria.NombreCompleto = cliente.NombreCompleto;
                auditoria.Telefono = cliente.Telefono;
                auditoria.Direccion = cliente.Direccion;
                auditoria.Email = cliente.Email;
                auditoria.Password = null;
                auditoria.TipoUsuario = cliente.TipoUsuario;
                auditoria.Restablecer = cliente.Restablecer;
                auditoria.FechaRegistro = cliente.FechaRegistro;
                auditoria.Estado = cliente.Estado;

                _context.Clientes_Auditoria.Add(auditoria);
                _context.Clientes.Remove(cliente);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Customer deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The customer could not be deleted.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "An unexpected error occurred while deleting the customer.");
            }
        }



        [Authorize(Roles = "Customer")]
        [HttpPut("CambiarPassword")]
        public async Task<IActionResult> CambiarPassword(ChangePasswordRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { Message = "Password information is required." });
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
            {
                return BadRequest(new { Message = "The new password must be different from the current password." });
            }

            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId) ||
                    !userId.StartsWith("customer:", StringComparison.Ordinal))
                {
                    return Unauthorized(new { Message = "The customer session is invalid." });
                }

                string cedula = userId.Substring("customer:".Length);

                var cliente = await _context.Clientes.FirstOrDefaultAsync(c =>
                    c.Cedula == cedula &&
                    c.TipoUsuario == 3 &&
                    c.Estado == 'A');

                if (cliente == null)
                {
                    return Unauthorized(new { Message = "The customer account was not found." });
                }

                if (!VerifyCustomerPassword(cliente, request.CurrentPassword))
                {
                    return Unauthorized(new { Message = "The current password is incorrect." });
                }

                ClienteAuditoria auditoria = new ClienteAuditoria();
                auditoria.Accion = "CAMBIO_CONTRASENA";
                auditoria.FechaCambio = DateTime.Now;
                auditoria.Cedula = cliente.Cedula;
                auditoria.TipoCedula = cliente.TipoCedula;
                auditoria.NombreCompleto = cliente.NombreCompleto;
                auditoria.Telefono = cliente.Telefono;
                auditoria.Direccion = cliente.Direccion;
                auditoria.Email = cliente.Email;
                auditoria.Password = null;
                auditoria.TipoUsuario = cliente.TipoUsuario;
                auditoria.Restablecer = cliente.Restablecer;
                auditoria.FechaRegistro = cliente.FechaRegistro;
                auditoria.Estado = cliente.Estado;

                cliente.Password = HashCustomerPassword(cliente, request.NewPassword);
                cliente.Restablecer = 1;

                _context.Clientes_Auditoria.Add(auditoria);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Password changed successfully."
                });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(
                    statusCode: 500,
                    title: "The password could not be changed.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(
                    statusCode: 500,
                    title: "An unexpected error occurred while changing the password.");
            }
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("ResetCustomerPassword")]
        public async Task<IActionResult> ResetCustomerPassword(ResetCustomerPasswordRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { Message = "Password information is required." });
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (!request.IdentityVerified)
            {
                return BadRequest(new { Message = "Verify the customer's identity in person before resetting the password." });
            }

            if (string.IsNullOrWhiteSpace(request.Cedula))
            {
                return BadRequest(new { Message = "The customer identification number is required." });
            }

            try
            {
                string customerId = request.Cedula.Trim();

                var customer = await _context.Clientes.FirstOrDefaultAsync(c => c.Cedula == customerId && c.TipoUsuario == 3);

                if (customer == null)
                {
                    return NotFound(new { Message = "Customer not found." });
                }

                if (customer.Estado != 'A')
                {
                    return Conflict(new { Message = "The customer account is inactive." });
                }

                ClienteAuditoria audit = new ClienteAuditoria();
                audit.Accion = "PASSWORD_RESET";
                audit.FechaCambio = DateTime.Now;
                audit.Cedula = customer.Cedula;
                audit.TipoCedula = customer.TipoCedula;
                audit.NombreCompleto = customer.NombreCompleto;
                audit.Telefono = customer.Telefono;
                audit.Direccion = customer.Direccion;
                audit.Email = customer.Email;
                audit.Password = null;
                audit.TipoUsuario = customer.TipoUsuario;
                audit.Restablecer = customer.Restablecer;
                audit.FechaRegistro = customer.FechaRegistro;
                audit.Estado = customer.Estado;

                customer.Password = HashCustomerPassword(customer, request.NewPassword);
                customer.Restablecer = 1;

                _context.Clientes_Auditoria.Add(audit);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Password reset successfully. The customer can now sign in with the new password." });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The password could not be reset.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "An unexpected error occurred while resetting the password.");
            }
        }


        [AllowAnonymous]
        [HttpPost("AutenticarPW")]
        public async Task<IActionResult> AutenticarPW([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return Unauthorized();
            }

            try
            {
                string normalizedEmail = request.Email.Trim().ToUpperInvariant();

                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Email.Trim().ToUpper() == normalizedEmail && c.Estado == 'A' && c.TipoUsuario == 3);

                if (cliente == null || !VerifyCustomerPassword(cliente, request.Password))
                {
                    return Unauthorized();
                }

                if (!cliente.Password.StartsWith("IdentityV3:", StringComparison.Ordinal))
                {
                    cliente.Password = HashCustomerPassword(cliente, request.Password);
                    await _context.SaveChangesAsync();
                }

                var autorizado = await autorizacionService.DevolverToken(cliente);

                if (autorizado == null)
                {
                    return Unauthorized();
                }

                return Ok(autorizado);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "Unable to sign in. Please try again.");
            }
        }


        private string HashCustomerPassword(Cliente cliente, string password)
        {
            var hasher = new PasswordHasher<Cliente>();
            return "IdentityV3:" + hasher.HashPassword(cliente, password);
        }

        private bool VerifyCustomerPassword(Cliente cliente, string password)
        {
            if (string.IsNullOrEmpty(cliente.Password) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            const string prefix = "IdentityV3:";

            if (cliente.Password.StartsWith(prefix, StringComparison.Ordinal))
            {
                try
                {
                    var hasher = new PasswordHasher<Cliente>();
                    string hash = cliente.Password.Substring(prefix.Length);
                    var result = hasher.VerifyHashedPassword(cliente, hash, password);

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

            byte[] storedPassword = Encoding.UTF8.GetBytes(cliente.Password);
            byte[] suppliedPassword = Encoding.UTF8.GetBytes(password);

            return CryptographicOperations.FixedTimeEquals(storedPassword, suppliedPassword);
        }

    }//end class
}//end namespace

using APIHotelBeach.Context;
using APIHotelBeach.DTOs;
using APIHotelBeach.Models;
using APIHotelBeach.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace APIHotelBeach.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmpleadosController : Controller
    {
        private readonly DbContextHotel _context;
        private readonly IAutorizacionServicesEmpleado autorizacionService;

        public EmpleadosController(DbContextHotel pContext, IAutorizacionServicesEmpleado autorizacionService)
        {
            _context = pContext;
            this.autorizacionService = autorizacionService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("Listado")]
        public async Task<IActionResult> Listado()
        {
            if (!await IsActiveAdministratorAsync())
            {
                return Forbid();
            }

            try
            {
                var employees = await _context.Empleados.AsNoTracking().OrderBy(e => e.NombreCompleto).ThenBy(e => e.ID).Select(e => new
                {
                    e.ID,
                    e.NombreCompleto,
                    e.Email,
                    e.TipoUsuario,
                    e.FechaRegistro,
                    e.Estado
                }).ToListAsync();

                return Ok(employees);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The employee list could not be loaded.");
            }
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("EmpleadoLogin")]
        public async Task<IActionResult> DatosEmpleado()
        {
            int? employeeId = GetCurrentEmployeeId();

            if (employeeId == null)
            {
                return Unauthorized();
            }

            var employee = await _context.Empleados.AsNoTracking().Where(e => e.ID == employeeId.Value && e.Estado == 'A' && (e.TipoUsuario == 1 || e.TipoUsuario == 2)).Select(e => new
            {
                e.ID,
                e.NombreCompleto,
                e.Email,
                e.TipoUsuario,
                e.Estado
            }).FirstOrDefaultAsync();

            if (employee == null)
            {
                return Unauthorized();
            }

            return Ok(employee);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("Consultar")]
        public async Task<IActionResult> Consultar(int ID)
        {
            if (!await IsActiveAdministratorAsync())
            {
                return Forbid();
            }

            if (ID <= 0)
            {
                return BadRequest(new { Message = "A valid employee ID is required." });
            }

            var employee = await _context.Empleados.AsNoTracking().Where(e => e.ID == ID).Select(e => new
            {
                e.ID,
                e.NombreCompleto,
                e.Email,
                e.TipoUsuario,
                e.FechaRegistro,
                e.Estado
            }).FirstOrDefaultAsync();

            if (employee == null)
            {
                return NotFound(new { Message = "Employee not found." });
            }

            return Ok(employee);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Agregar")]
        public async Task<IActionResult> Agregar(EmployeeCreateRequest request)
        {
            if (!await IsActiveAdministratorAsync())
            {
                return Forbid();
            }

            if (request == null)
            {
                return BadRequest(new { Message = "Employee information is required." });
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (request.Estado != 'A' && request.Estado != 'I')
            {
                return BadRequest(new { Message = "Select a valid account status." });
            }

            try
            {
                string email = request.Email.Trim();
                string normalizedEmail = email.ToUpperInvariant();

                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                bool emailExists = await _context.Empleados.AnyAsync(e => e.Email.Trim().ToUpper() == normalizedEmail);

                if (emailExists)
                {
                    return Conflict(new { Message = "That email address is already registered to another staff account." });
                }

                Empleado employee = new Empleado();
                employee.NombreCompleto = request.NombreCompleto.Trim();
                employee.Email = email;
                employee.TipoUsuario = 2;
                employee.FechaRegistro = DateTime.Now;
                employee.Estado = request.Estado;
                employee.Password = HashEmployeePassword(employee, request.Password);

                _context.Empleados.Add(employee);
                await _context.SaveChangesAsync();

                _context.Empleados_Auditoria.Add(CreateAudit(employee, "CREATED"));
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { Message = "Employee created successfully.", EmployeeId = employee.ID });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The employee could not be saved.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "An unexpected error occurred while creating the employee.");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("Modificar")]
        public async Task<IActionResult> Modificar(EmployeeEditRequest request)
        {
            if (!await IsActiveAdministratorAsync())
            {
                return Forbid();
            }

            if (request == null)
            {
                return BadRequest(new { Message = "Employee information is required." });
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (request.Estado != 'A' && request.Estado != 'I')
            {
                return BadRequest(new { Message = "Select a valid account status." });
            }

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                var employee = await _context.Empleados.FirstOrDefaultAsync(e => e.ID == request.ID);

                if (employee == null)
                {
                    return NotFound(new { Message = "Employee not found." });
                }

                if (employee.ID == GetCurrentEmployeeId() && request.Estado != 'A')
                {
                    return Conflict(new { Message = "You cannot deactivate your own account." });
                }

                if (employee.TipoUsuario == 1 && employee.Estado == 'A' && request.Estado != 'A')
                {
                    bool anotherAdministratorExists = await _context.Empleados.AnyAsync(e => e.ID != employee.ID && e.TipoUsuario == 1 && e.Estado == 'A');

                    if (!anotherAdministratorExists)
                    {
                        return Conflict(new { Message = "The last active administrator cannot be deactivated." });
                    }
                }

                string email = request.Email.Trim();
                string normalizedEmail = email.ToUpperInvariant();

                bool emailExists = await _context.Empleados.AnyAsync(e => e.ID != employee.ID && e.Email.Trim().ToUpper() == normalizedEmail);

                if (emailExists)
                {
                    return Conflict(new { Message = "That email address is already registered to another staff account." });
                }

                var audit = CreateAudit(employee, "UPDATED");

                employee.NombreCompleto = request.NombreCompleto.Trim();
                employee.Email = email;
                employee.Estado = request.Estado;

                if (!string.IsNullOrEmpty(request.NewPassword))
                {
                    employee.Password = HashEmployeePassword(employee, request.NewPassword);
                }

                _context.Empleados_Auditoria.Add(audit);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { Message = "Employee updated successfully." });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The employee changes could not be saved.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "An unexpected error occurred while updating the employee.");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("Eliminar")]
        public async Task<IActionResult> Eliminar(int ID)
        {
            if (!await IsActiveAdministratorAsync())
            {
                return Forbid();
            }

            if (ID <= 0)
            {
                return BadRequest(new { Message = "A valid employee ID is required." });
            }

            if (ID == GetCurrentEmployeeId())
            {
                return Conflict(new { Message = "You cannot delete your own account." });
            }

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                var employee = await _context.Empleados.FirstOrDefaultAsync(e => e.ID == ID);

                if (employee == null)
                {
                    return NotFound(new { Message = "Employee not found." });
                }

                if (employee.TipoUsuario == 1 && employee.Estado == 'A')
                {
                    bool anotherAdministratorExists = await _context.Empleados.AnyAsync(e => e.ID != employee.ID && e.TipoUsuario == 1 && e.Estado == 'A');

                    if (!anotherAdministratorExists)
                    {
                        return Conflict(new { Message = "The last active administrator cannot be deleted." });
                    }
                }

                _context.Empleados_Auditoria.Add(CreateAudit(employee, "DELETED"));
                _context.Empleados.Remove(employee);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Employee deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The employee could not be deleted. Check whether other records depend on this account.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "An unexpected error occurred while deleting the employee.");
            }
        }

        [AllowAnonymous]
        [HttpPost("AutenticarPW")]
        public async Task<IActionResult> AutenticarPW([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return Unauthorized(new { Message = "Invalid email or password." });
            }

            try
            {
                Empleado credentials = new Empleado();
                credentials.Email = request.Email.Trim();
                credentials.Password = request.Password;

                var authorization = await autorizacionService.DevolverTokenEmpleado(credentials);

                if (authorization == null || !authorization.Resultado || string.IsNullOrWhiteSpace(authorization.Token))
                {
                    return Unauthorized(new { Message = "Invalid email or password." });
                }

                return Ok(authorization);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "Unable to sign in. Please try again.");
            }
        }

        private int? GetCurrentEmployeeId()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId) || !userId.StartsWith("employee:", StringComparison.Ordinal))
            {
                return null;
            }

            if (!int.TryParse(userId.Substring("employee:".Length), out int employeeId) || employeeId <= 0)
            {
                return null;
            }

            return employeeId;
        }

        private async Task<bool> IsActiveAdministratorAsync()
        {
            int? employeeId = GetCurrentEmployeeId();

            if (employeeId == null)
            {
                return false;
            }

            return await _context.Empleados.AnyAsync(e => e.ID == employeeId.Value && e.TipoUsuario == 1 && e.Estado == 'A');
        }

        private string HashEmployeePassword(Empleado employee, string password)
        {
            var hasher = new PasswordHasher<Empleado>();
            return "IdentityV3:" + hasher.HashPassword(employee, password);
        }

        private EmpleadoAuditoria CreateAudit(Empleado employee, string action)
        {
            EmpleadoAuditoria audit = new EmpleadoAuditoria();
            audit.Accion = action;
            audit.FechaCambio = DateTime.Now;
            audit.ID = employee.ID;
            audit.NombreCompleto = employee.NombreCompleto;
            audit.Email = employee.Email;
            audit.Password = null;
            audit.TipoUsuario = employee.TipoUsuario;
            audit.FechaRegistro = employee.FechaRegistro;
            audit.Estado = employee.Estado;

            return audit;
        }
    }
}
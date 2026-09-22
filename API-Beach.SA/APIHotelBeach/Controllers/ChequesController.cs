using APIHotelBeach.Context;
using APIHotelBeach.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APIHotelBeach.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChequesController : Controller
    {
        private readonly DbContextHotel _context;

        public ChequesController(DbContextHotel pContext)
        {
            _context = pContext;
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("Listado")]
        public async Task<List<Cheque>> Listado()
        {
            var list = await _context.Cheques.ToListAsync();

            return list;
        }

        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpGet("Consultar")]
        public async Task<ActionResult<Cheque>> Consultar(int Id)
        {
            if (Id <= 0)
            {
                return BadRequest(new { Message = "Invalid reservation ID." });
            }

            var consulta = _context.Reservaciones.AsNoTracking().Where(r => r.Id == Id);

            if (!User.IsInRole("Admin") && !User.IsInRole("Employee"))
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId) || !userId.StartsWith("customer:", StringComparison.Ordinal))
                {
                    return Forbid();
                }

                string cedula = userId.Substring("customer:".Length);

                if (string.IsNullOrWhiteSpace(cedula))
                {
                    return Forbid();
                }

                consulta = consulta.Where(r => r.CedulaCliente == cedula);
            }

            var reserva = await consulta.FirstOrDefaultAsync();

            if (reserva == null)
            {
                return NotFound(new { Message = "Reservation not found." });
            }

            var cheque = await _context.Cheques.AsNoTracking().FirstOrDefaultAsync(c => c.IdReservacion == reserva.Id);

            if (cheque == null)
            {
                return NotFound(new { Message = "No check is registered for this reservation." });
            }

            return Ok(cheque);
        }

        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpPut("Modificar")]
        public async Task<IActionResult> Modificar(Cheque pCheque)
        {
            if (pCheque == null || pCheque.IdCheque <= 0)
            {
                return BadRequest(new { Message = "Invalid check ID." });
            }

            if (pCheque.NumeroCheque <= 0 || string.IsNullOrWhiteSpace(pCheque.NombreBanco))
            {
                return BadRequest(new { Message = "Enter a positive check number and a bank name." });
            }

            try
            {
                var cheque = await _context.Cheques.FirstOrDefaultAsync(c => c.IdCheque == pCheque.IdCheque);

                if (cheque == null)
                {
                    return NotFound(new { Message = "Check not found." });
                }

                var consulta = _context.Reservaciones.Where(r => r.Id == cheque.IdReservacion);

                if (!User.IsInRole("Admin") && !User.IsInRole("Employee"))
                {
                    string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (string.IsNullOrWhiteSpace(userId) || !userId.StartsWith("customer:", StringComparison.Ordinal))
                    {
                        return Forbid();
                    }

                    string cedula = userId.Substring("customer:".Length);

                    if (string.IsNullOrWhiteSpace(cedula))
                    {
                        return Forbid();
                    }

                    consulta = consulta.Where(r => r.CedulaCliente == cedula);
                }

                var reserva = await consulta.FirstOrDefaultAsync();

                if (reserva == null)
                {
                    return NotFound(new { Message = "Reservation not found." });
                }

                if (reserva.Estado != 'A' || reserva.TipoPago != "Cheque")
                {
                    return BadRequest(new { Message = "The reservation must be active and use check payment." });
                }

                if (pCheque.IdReservacion != cheque.IdReservacion)
                {
                    return BadRequest(new { Message = "The check cannot be moved to another reservation." });
                }

                bool numeroRegistrado = await _context.Cheques.AnyAsync(c => c.NumeroCheque == pCheque.NumeroCheque && c.IdCheque != cheque.IdCheque);

                if (numeroRegistrado)
                {
                    return Conflict(new { Message = "That check number is already registered." });
                }

                cheque.NumeroCheque = pCheque.NumeroCheque;
                cheque.NombreBanco = pCheque.NombreBanco.Trim();

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Check updated successfully." });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The check changes could not be saved.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "An unexpected error occurred while updating the check.");
            }
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpDelete("Eliminar")]
        public async Task<string> Eliminar(int Id)
        {
            string mensaje = "";
            try
            {
                var temp = await _context.Cheques.FirstOrDefaultAsync(c => c.IdReservacion == Id);
                if (temp == null)
                {
                    mensaje = "No existe ningun cheque con el numero de reservacion " + Id;
                }
                else
                {
                    _context.Cheques.Remove(temp);
                    await _context.SaveChangesAsync();
                    mensaje = $"Cheque con el numero {temp.NumeroCheque}, eliminado correctamente";
                }
            }
            catch (Exception ex)
            {
                mensaje = "Error " + ex.Message + " " + ex.InnerException.ToString();
            }
            return mensaje;
        }
    }
}

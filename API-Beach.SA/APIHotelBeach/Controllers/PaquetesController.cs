using APIHotelBeach.Context;
using APIHotelBeach.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Identity.Client.AppConfig;

namespace APIHotelBeach.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaquetesController : Controller
    {
        private readonly DbContextHotel _context;

        public PaquetesController(DbContextHotel pContext)
        {
            _context = pContext;
        }//end PaquetesController

        //[Authorize] 
        [HttpGet("Listado")]
        public async Task<List<Paquete>> Listado()
        {
            var list = await _context.Paquetes.ToListAsync();
            if (list == null)
            {
                return new List<Paquete>();
            }
            else
            {
                return list;
            }//end if/else
        }//end Listado

        //[Authorize]
        [HttpGet("Consultar")]
        public async Task<Paquete> Consultar(int ID)
        {
            var temp = await _context.Paquetes.FirstOrDefaultAsync(p => p.ID == ID);
            return temp;
        }//end Consultar

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost("Agregar")]
        public async Task<IActionResult> Agregar(Paquete paquete)
        {
            paquete.ID = 0;
            paquete.FechaRegistro = DateTime.Now;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Paquetes.Add(paquete);
                await _context.SaveChangesAsync();

                var audit = new PaqueteAuditoria
                {
                    Accion = "AGREGADO",
                    FechaCambio = DateTime.Now,
                    ID = paquete.ID,
                    NombrePaquete = paquete.NombrePaquete,
                    Precio = paquete.Precio,
                    PorcentajePrima = paquete.PorcentajePrima,
                    LimiteMeses = paquete.LimiteMeses,
                    FechaRegistro = paquete.FechaRegistro,
                    Estado = paquete.Estado
                };

                _context.Paquetes_Auditoria.Add(audit);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return StatusCode(201, new { Message = "Package created successfully.", Id = paquete.ID });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return Problem(statusCode: 500, title: "Unable to create the package.");
            }
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("Modificar")]
        public async Task<IActionResult> Modificar(Paquete paquete)
        {
            if (paquete.ID <= 0)
            {
                return BadRequest(new { Message = "A valid package ID is required." });
            }

            var existingPackage = await _context.Paquetes.FirstOrDefaultAsync(p => p.ID == paquete.ID);

            if (existingPackage == null)
            {
                return NotFound(new { Message = "Package not found." });
            }

            var audit = new PaqueteAuditoria
            {
                Accion = "MODIFICADO",
                FechaCambio = DateTime.Now,
                ID = existingPackage.ID,
                NombrePaquete = existingPackage.NombrePaquete,
                Precio = existingPackage.Precio,
                PorcentajePrima = existingPackage.PorcentajePrima,
                LimiteMeses = existingPackage.LimiteMeses,
                FechaRegistro = existingPackage.FechaRegistro,
                Estado = existingPackage.Estado
            };

            existingPackage.NombrePaquete = paquete.NombrePaquete;
            existingPackage.Precio = paquete.Precio;
            existingPackage.PorcentajePrima = paquete.PorcentajePrima;
            existingPackage.LimiteMeses = paquete.LimiteMeses;
            existingPackage.Estado = paquete.Estado;

            _context.Paquetes_Auditoria.Add(audit);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Package updated successfully." });
            }
            catch (DbUpdateException)
            {
                return Problem(statusCode: 500, title: "Unable to update the package.");
            }
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpDelete("Eliminar")]
        public async Task<IActionResult> Eliminar(int ID)
        {
            if (ID <= 0)
            {
                return BadRequest(new { Message = "Invalid package ID." });
            }

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                var paquete = await _context.Paquetes.FirstOrDefaultAsync(p => p.ID == ID);

                if (paquete == null)
                {
                    return NotFound(new { Message = "Package not found." });
                }

                bool tieneReservaciones = await _context.Reservaciones.AnyAsync(r => r.IdPaquete == ID);

                if (tieneReservaciones)
                {
                    return Conflict(new { Message = "This package has reservations and cannot be deleted. You can change its status to Inactive instead." });
                }

                var auditoria = new PaqueteAuditoria
                {
                    Accion = "ELIMINADO",
                    FechaCambio = DateTime.Now,
                    ID = paquete.ID,
                    NombrePaquete = paquete.NombrePaquete,
                    Precio = paquete.Precio,
                    PorcentajePrima = paquete.PorcentajePrima,
                    LimiteMeses = paquete.LimiteMeses,
                    FechaRegistro = paquete.FechaRegistro,
                    Estado = paquete.Estado
                };

                _context.Paquetes_Auditoria.Add(auditoria);
                _context.Paquetes.Remove(paquete);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { Message = "The package changed during this operation. Refresh the page and try again." });
            }
            catch (DbUpdateException)
            {
                return Problem(statusCode: 500, title: "Unable to delete the package.");
            }
        }

    }//end class
}//end namespace

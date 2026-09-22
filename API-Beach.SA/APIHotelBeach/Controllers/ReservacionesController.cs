using APIHotelBeach.Context;
using APIHotelBeach.Models;
using APIHotelBeach.ViewModels;
using iText.Commons.Actions.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Metrics;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using System.Security.Claims;
using System.Data;

namespace APIHotelBeach.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReservacionesController : Controller
    {

        private readonly DbContextHotel _context;

        /// <summary>
        /// Constructor con parámetros
        /// </summary>
        /// <param name="pContext"></param>
        public ReservacionesController(DbContextHotel pContext)
        {
            _context = pContext;
        }

        //***   MÉTODOS     CRUD ***

        //lista de todas las reservaciones
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("ListaReservas")]
        public async Task<ActionResult<List<Reservacion>>> ListaReservas()
        {
            var reservaciones = await _context.Reservaciones.AsNoTracking().OrderByDescending(r => r.FechaReserva).ThenByDescending(r => r.Id).ToListAsync();

            return Ok(reservaciones);
        }

        //lista de reservaciones de un cliente especifico
        [Authorize(Roles = "Customer")]
        [HttpGet("ListaReservasCliente")]
        public async Task<ActionResult<List<Reservacion>>> ListaReservasCliente()
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

            var reservaciones = await _context.Reservaciones.AsNoTracking().Where(r => r.CedulaCliente == cedula).OrderByDescending(r => r.FechaReserva).ThenByDescending(r => r.Id).ToListAsync();

            return Ok(reservaciones);
        }

        //Agregar reservacion
        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpPost("AgregarReserva")]
        public async Task<IActionResult> AgregarReserva(Reservacion pReserva)
        {
            if (pReserva == null)
            {
                return BadRequest(new
                {
                    Message = "Reservation data is required."
                });
            }

            if (pReserva.Duracion <= 0)
            {
                return BadRequest(new
                {
                    Message = "Stay duration must be greater than zero."
                });
            }

            if (string.IsNullOrWhiteSpace(pReserva.CedulaCliente))
            {
                return BadRequest(new
                {
                    Message = "Customer identification is required."
                });
            }

            if (pReserva.IdPaquete <= 0)
            {
                return BadRequest(new
                {
                    Message = "A valid package is required."
                });
            }

            if (string.IsNullOrWhiteSpace(pReserva.TipoPago))
            {
                return BadRequest(new
                {
                    Message = "Payment method is required."
                });
            }

            try
            {
                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Cedula == pReserva.CedulaCliente);

                if (cliente == null)
                {
                    return NotFound(new
                    {
                        Message = "Customer not found."
                    });
                }

                if (cliente.Estado != 'A')
                {
                    return Conflict(new
                    {
                        Message = "The customer is inactive."
                    });
                }

                var paquete = await _context.Paquetes.FirstOrDefaultAsync(p => p.ID == pReserva.IdPaquete);

                if (paquete == null)
                {
                    return NotFound(new
                    {
                        Message = "Package not found."
                    });
                }

                if (paquete.Estado != 'A')
                {
                    return Conflict(new
                    {
                        Message = "The selected package is inactive."
                    });
                }

                await using var connection = _context.Database.GetDbConnection();

                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                await using var command = connection.CreateCommand();

                command.CommandText = "SELECT NEXT VALUE FOR dbo.ReservationNumberSequence";

                var nextId = await command.ExecuteScalarAsync();

                pReserva.Id = Convert.ToInt32(nextId);

                pReserva.Estado = 'A';
                pReserva.Subtotal = paquete.Precio * pReserva.Duracion;
                pReserva.Impuesto = Math.Round(pReserva.Subtotal * 0.13m, 2);
                pReserva.Descuento = 0;

                if (string.Equals(pReserva.TipoPago, "Efectivo", StringComparison.OrdinalIgnoreCase))
                {
                    if (pReserva.Duracion >= 3 && pReserva.Duracion <= 6)
                    {
                        pReserva.Descuento = Math.Round(pReserva.Subtotal * 0.10m, 2);
                    }
                    else if (pReserva.Duracion >= 7 && pReserva.Duracion <= 9)
                    {
                        pReserva.Descuento = Math.Round(pReserva.Subtotal * 0.15m, 2);
                    }
                    else if (pReserva.Duracion >= 10 && pReserva.Duracion <= 12)
                    {
                        pReserva.Descuento = Math.Round(pReserva.Subtotal * 0.20m, 2);
                    }
                    else if (pReserva.Duracion >= 13)
                    {
                        pReserva.Descuento = Math.Round(pReserva.Subtotal * 0.25m, 2);
                    }
                }

                pReserva.MontoTotal = Math.Round(pReserva.Subtotal - pReserva.Descuento + pReserva.Impuesto, 2);

                if (paquete.LimiteMeses == 0)
                {
                    pReserva.Adelanto = pReserva.MontoTotal;
                    pReserva.MontoMensualidad = 0;
                }
                else
                {
                    pReserva.Adelanto = Math.Round(pReserva.MontoTotal * paquete.PorcentajePrima / 100, 2);
                    pReserva.MontoMensualidad = Math.Round((pReserva.MontoTotal - pReserva.Adelanto) / paquete.LimiteMeses, 2);
                }

                _context.Reservaciones.Add(pReserva);

                var reservacionAuditoria = new ReservacionAuditoria
                {
                    Accion = "AGREGADO",
                    FechaCambio = DateTime.Now,
                    Id = pReserva.Id,
                    CedulaCliente = pReserva.CedulaCliente,
                    IdPaquete = pReserva.IdPaquete,
                    TipoPago = pReserva.TipoPago,
                    FechaReserva = pReserva.FechaReserva,
                    Duracion = pReserva.Duracion,
                    Subtotal = pReserva.Subtotal,
                    Impuesto = pReserva.Impuesto,
                    Descuento = pReserva.Descuento,
                    MontoTotal = pReserva.MontoTotal,
                    Adelanto = pReserva.Adelanto,
                    MontoMensualidad = pReserva.MontoMensualidad,
                    Estado = pReserva.Estado
                };

                _context.Reservaciones_Auditoria.Add(reservacionAuditoria);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Reservation created successfully.",
                    ReservationId = pReserva.Id
                });
            }
            catch (DbUpdateException)
            {
                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "The reservation could not be saved."
                );
            }
            catch (Exception ex)
            {
                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "An unexpected error occurred while creating the reservation.",
                    detail: ex.ToString()
                );
            }
        }

        //BuscarReserva
        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpGet("BuscarReserva")]
        public async Task<ActionResult<Reservacion>> BuscarReserva(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { Message = "Invalid reservation ID." });
            }

            var consulta = _context.Reservaciones.AsNoTracking().Where(r => r.Id == id);

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

            var reservacion = await consulta.FirstOrDefaultAsync();

            if (reservacion == null)
            {
                return NotFound(new { Message = "Reservation not found." });
            }

            return Ok(reservacion);
        }

        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpGet("DownloadReceipt")]
        public async Task<IActionResult> DownloadReceipt(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { Message = "A valid reservation ID is required." });
            }

            try
            {
                var query = _context.Reservaciones.AsNoTracking().Where(r => r.Id == id);

                if (!User.IsInRole("Admin") && !User.IsInRole("Employee"))
                {
                    string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (string.IsNullOrWhiteSpace(userId) || !userId.StartsWith("customer:", StringComparison.Ordinal))
                    {
                        return Forbid();
                    }

                    string customerId = userId.Substring("customer:".Length);

                    if (string.IsNullOrWhiteSpace(customerId))
                    {
                        return Forbid();
                    }

                    query = query.Where(r => r.CedulaCliente == customerId);
                }

                var reservation = await query.FirstOrDefaultAsync();

                if (reservation == null)
                {
                    return NotFound(new { Message = "The reservation was not found." });
                }

                var customer = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Cedula == reservation.CedulaCliente);

                if (customer == null)
                {
                    return NotFound(new { Message = "The customer was not found." });
                }

                decimal exchangeRate;

                try
                {
                    using var exchangeClient = new TipoCambioAPI().Inicial();
                    exchangeClient.Timeout = TimeSpan.FromSeconds(5);

                    using var exchangeResponse = await exchangeClient.GetAsync("/tdc/tdc.json");

                    if (!exchangeResponse.IsSuccessStatusCode)
                    {
                        return StatusCode(503, new { Message = "The exchange rate is temporarily unavailable. Please try again later." });
                    }

                    string exchangeContent = await exchangeResponse.Content.ReadAsStringAsync();
                    var exchangeData = JsonConvert.DeserializeObject<TipoCambio>(exchangeContent);

                    if (exchangeData == null || exchangeData.venta <= 0)
                    {
                        return StatusCode(503, new { Message = "The exchange rate is temporarily unavailable. Please try again later." });
                    }

                    exchangeRate = exchangeData.venta;
                }
                catch (HttpRequestException)
                {
                    return StatusCode(503, new { Message = "The exchange rate service is unavailable. Please try again later." });
                }
                catch (TaskCanceledException)
                {
                    return StatusCode(503, new { Message = "The exchange rate service took too long to respond. Please try again." });
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    return StatusCode(503, new { Message = "The exchange rate service returned an invalid response." });
                }

                ReservationReceiptData receipt = new ReservationReceiptData();
                receipt.Id = reservation.Id;
                receipt.CedulaCliente = reservation.CedulaCliente;
                receipt.Email = customer.Email;
                receipt.NombreCompleto = customer.NombreCompleto;
                receipt.IdPaquete = reservation.IdPaquete;
                receipt.TipoPago = reservation.TipoPago;
                receipt.FechaReserva = reservation.FechaReserva;
                receipt.Duracion = reservation.Duracion;
                receipt.Subtotal = reservation.Subtotal;
                receipt.Impuesto = reservation.Impuesto;
                receipt.Descuento = reservation.Descuento;
                receipt.MontoTotal = reservation.MontoTotal;
                receipt.Adelanto = reservation.Adelanto;
                receipt.MontoMensualidad = reservation.MontoMensualidad;
                receipt.TipoCambio = Math.Round(reservation.MontoTotal * exchangeRate, 2);

                ReservationPdfGenerator generator = new ReservationPdfGenerator();
                byte[] pdf;

                if (string.Equals(reservation.TipoPago, "Cheque", StringComparison.OrdinalIgnoreCase))
                {
                    var check = await _context.Cheques.AsNoTracking().FirstOrDefaultAsync(c => c.IdReservacion == reservation.Id);

                    if (check == null)
                    {
                        return Conflict(new { Message = "Register the check details before downloading this receipt." });
                    }

                    ReservaPDFCheque checkReceipt = new ReservaPDFCheque();
                    checkReceipt.Id = receipt.Id;
                    checkReceipt.CedulaCliente = receipt.CedulaCliente;
                    checkReceipt.Email = receipt.Email;
                    checkReceipt.NombreCompleto = receipt.NombreCompleto;
                    checkReceipt.IdPaquete = receipt.IdPaquete;
                    checkReceipt.TipoPago = receipt.TipoPago;
                    checkReceipt.FechaReserva = receipt.FechaReserva;
                    checkReceipt.Duracion = receipt.Duracion;
                    checkReceipt.Subtotal = receipt.Subtotal;
                    checkReceipt.Impuesto = receipt.Impuesto;
                    checkReceipt.Descuento = receipt.Descuento;
                    checkReceipt.MontoTotal = receipt.MontoTotal;
                    checkReceipt.Adelanto = receipt.Adelanto;
                    checkReceipt.MontoMensualidad = receipt.MontoMensualidad;
                    checkReceipt.TipoCambio = receipt.TipoCambio;
                    checkReceipt.NumeroCheque = check.NumeroCheque;
                    checkReceipt.NombreBanco = check.NombreBanco;

                    pdf = generator.GenerateCheck(checkReceipt);
                }
                else
                {
                    pdf = generator.GenerateReservation(receipt);
                }

                Response.Headers["Cache-Control"] = "no-store";

                return File(pdf, "application/pdf", $"Reservation-{reservation.Id}.pdf");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The reservation receipt could not be generated.");
            }
        }


        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpPost("AgregarCheque")]
        public async Task<IActionResult> AgregarCheque(CheckCreateRequest request)
        {
            if (request == null)
            {
                return BadRequest("The check information is required.");
            }

            if (request.IdReservacion <= 0)
            {
                return BadRequest("A valid reservation is required.");
            }

            if (request.NumeroCheque <= 0)
            {
                return BadRequest("A positive check number is required.");
            }

            if (string.IsNullOrWhiteSpace(request.NombreBanco))
            {
                return BadRequest("The bank name is required.");
            }

            try
            {
                var consulta = _context.Reservaciones.Where(r => r.Id == request.IdReservacion);

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
                    return NotFound("The reservation was not found.");
                }

                if (reserva.Estado != 'A')
                {
                    return BadRequest("The reservation must be active.");
                }

                if (!string.Equals(reserva.TipoPago, "Cheque", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("The reservation payment method is not check.");
                }

                bool chequeExistente = await _context.Cheques.AnyAsync(c => c.IdReservacion == reserva.Id);

                if (chequeExistente)
                {
                    return Conflict("A check is already registered for this reservation.");
                }

                int checkId;

                await _context.Database.OpenConnectionAsync();

                try
                {
                    using var command = _context.Database.GetDbConnection().CreateCommand();
                    command.CommandText = "SELECT NEXT VALUE FOR dbo.CheckNumberSequence";

                    object result = await command.ExecuteScalarAsync();
                    checkId = Convert.ToInt32(result);
                }
                finally
                {
                    await _context.Database.CloseConnectionAsync();
                }

                Cheque cheque = new Cheque();
                cheque.IdCheque = checkId;
                cheque.NumeroCheque = request.NumeroCheque;
                cheque.NombreBanco = request.NombreBanco.Trim();
                cheque.IdReservacion = reserva.Id;

                _context.Cheques.Add(cheque);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Check registered successfully.", CheckId = cheque.IdCheque });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The check could not be saved.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "An unexpected error occurred while registering the check.");
            }
        }

        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpPut("Editar")]
        public async Task<IActionResult> Editar(Reservacion pReserva)
        {
            if (pReserva == null || pReserva.Id <= 0)
            {
                return BadRequest(new { Message = "Invalid reservation ID." });
            }

            try
            {
                var consulta = _context.Reservaciones.Where(r => r.Id == pReserva.Id);

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

                if (reserva.Estado != 'A')
                {
                    return BadRequest(new { Message = "Only active reservations can be edited." });
                }

                if (pReserva.Duracion <= 0)
                {
                    return BadRequest(new { Message = "The stay must last at least one night." });
                }

                if (pReserva.FechaReserva == default)
                {
                    return BadRequest(new { Message = "A reservation date is required." });
                }

                if (pReserva.TipoPago != "Cheque" && pReserva.TipoPago != "Tarjeta" && pReserva.TipoPago != "Efectivo")
                {
                    return BadRequest(new { Message = "Select a valid payment method." });
                }

                var paquete = await _context.Paquetes.FirstOrDefaultAsync(p => p.ID == pReserva.IdPaquete);

                if (paquete == null || paquete.Estado != 'A')
                {
                    return BadRequest(new { Message = "Select an active package." });
                }

                if (paquete.Precio < 0 || paquete.PorcentajePrima < 0 || paquete.PorcentajePrima > 100 || paquete.LimiteMeses < 0)
                {
                    return BadRequest(new { Message = "The package payment settings are invalid." });
                }

                bool tieneCheque = await _context.Cheques.AnyAsync(c => c.IdReservacion == reserva.Id);

                if (tieneCheque && pReserva.TipoPago != "Cheque")
                {
                    return Conflict(new { Message = "This reservation has a registered check. Its payment method cannot be changed until the check is resolved." });
                }

                ReservacionAuditoria auditoria = new ReservacionAuditoria();
                auditoria.Accion = "MODIFICADO";
                auditoria.FechaCambio = DateTime.Now;
                auditoria.Id = reserva.Id;
                auditoria.CedulaCliente = reserva.CedulaCliente;
                auditoria.IdPaquete = reserva.IdPaquete;
                auditoria.TipoPago = reserva.TipoPago;
                auditoria.FechaReserva = reserva.FechaReserva;
                auditoria.Duracion = reserva.Duracion;
                auditoria.Subtotal = reserva.Subtotal;
                auditoria.Impuesto = reserva.Impuesto;
                auditoria.Descuento = reserva.Descuento;
                auditoria.MontoTotal = reserva.MontoTotal;
                auditoria.Adelanto = reserva.Adelanto;
                auditoria.MontoMensualidad = reserva.MontoMensualidad;
                auditoria.Estado = reserva.Estado;

                reserva.IdPaquete = paquete.ID;
                reserva.TipoPago = pReserva.TipoPago;
                reserva.FechaReserva = pReserva.FechaReserva.Date;
                reserva.Duracion = pReserva.Duracion;

                reserva.Subtotal = Math.Round(paquete.Precio * reserva.Duracion, 2);
                reserva.Impuesto = Math.Round(reserva.Subtotal * 0.13m, 2);
                reserva.Descuento = 0;

                if (reserva.TipoPago == "Efectivo")
                {
                    decimal porcentajeDescuento = reserva.Duracion switch
                    {
                        >= 13 => 0.25m,
                        >= 10 => 0.20m,
                        >= 7 => 0.15m,
                        >= 3 => 0.10m,
                        _ => 0m
                    };

                    reserva.Descuento = Math.Round(reserva.Subtotal * porcentajeDescuento, 2);
                }

                reserva.MontoTotal = Math.Round(reserva.Subtotal - reserva.Descuento + reserva.Impuesto, 2);

                if (paquete.LimiteMeses == 0)
                {
                    reserva.Adelanto = reserva.MontoTotal;
                    reserva.MontoMensualidad = 0;
                }
                else
                {
                    reserva.Adelanto = Math.Round(reserva.MontoTotal * paquete.PorcentajePrima / 100, 2);
                    reserva.MontoMensualidad = Math.Round((reserva.MontoTotal - reserva.Adelanto) / paquete.LimiteMeses, 2);
                }

                _context.Reservaciones_Auditoria.Add(auditoria);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Reservation updated successfully.", ReservationId = reserva.Id, NeedsCheck = reserva.TipoPago == "Cheque" && !tieneCheque });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The reservation changes could not be saved.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "An unexpected error occurred while updating the reservation.");
            }
        }

        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpDelete("Eliminar")]
        public async Task<IActionResult> Eliminar(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { Message = "Invalid reservation ID." });
            }

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                var consulta = _context.Reservaciones.Where(r => r.Id == id);

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

                ReservacionAuditoria auditoria = new ReservacionAuditoria();
                auditoria.Accion = "ELIMINADO";
                auditoria.FechaCambio = DateTime.Now;
                auditoria.Id = reserva.Id;
                auditoria.CedulaCliente = reserva.CedulaCliente;
                auditoria.IdPaquete = reserva.IdPaquete;
                auditoria.TipoPago = reserva.TipoPago;
                auditoria.FechaReserva = reserva.FechaReserva;
                auditoria.Duracion = reserva.Duracion;
                auditoria.Subtotal = reserva.Subtotal;
                auditoria.Impuesto = reserva.Impuesto;
                auditoria.Descuento = reserva.Descuento;
                auditoria.MontoTotal = reserva.MontoTotal;
                auditoria.Adelanto = reserva.Adelanto;
                auditoria.MontoMensualidad = reserva.MontoMensualidad;
                auditoria.Estado = reserva.Estado;

                var cheques = await _context.Cheques.Where(c => c.IdReservacion == reserva.Id).ToListAsync();

                _context.Cheques.RemoveRange(cheques);
                await _context.SaveChangesAsync();

                _context.Reservaciones_Auditoria.Add(auditoria);
                await _context.SaveChangesAsync();

                _context.Reservaciones.Remove(reserva);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { Message = "Reservation deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "The reservation could not be deleted.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return Problem(statusCode: 500, title: "An unexpected error occurred while deleting the reservation.");
            }
        }

    }
}

using AppWebBeachSA.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;

namespace AppWebBeachSA.Controllers
{
    [Authorize(Roles = "Admin,Employee,Customer")]
    public class ChequesController : Controller
    {
        private HotelAPI hotelAPI;

        private HttpClient client;

        /// <summary>
        /// Metodo constructor
        /// </summary>
        public ChequesController()
        {
            hotelAPI = new HotelAPI();

            client = hotelAPI.Initial();
        }



        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind] Cheque pCheque)
        {
            var numeroReserva = TempData.Peek("NumeroReserva");

            if (numeroReserva == null || !int.TryParse(numeroReserva.ToString(), out int reservationId) || reservationId <= 0)
            {
                ModelState.AddModelError("", "The reservation could not be identified. Please return to your reservations.");
                return View(pCheque);
            }

            pCheque.IdReservacion = reservationId;

            if (!ModelState.IsValid)
            {
                return View(pCheque);
            }

            try
            {
                pCheque.IdCheque = 0;

                CheckCreateRequest request = new CheckCreateRequest();
                request.NumeroCheque = pCheque.NumeroCheque;
                request.NombreBanco = pCheque.NombreBanco;
                request.IdReservacion = pCheque.IdReservacion;

                client.DefaultRequestHeaders.Authorization = AutorizacionToken();

                HttpResponseMessage response = await client.PostAsJsonAsync("/Reservaciones/AgregarCheque", request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", User.IsInRole("Customer") ? "Clientes" : "Empleados");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    ModelState.AddModelError("", "You do not have permission to register this check.");
                    return View(pCheque);
                }

                if (response.IsSuccessStatusCode)
                {
                    TempData.Remove("NumeroReserva");

                    return RedirectToAction("Index", "Home");
                }

                string mensaje = response.StatusCode switch
                {
                    HttpStatusCode.BadRequest => "Check the bank information and reservation payment method.",
                    HttpStatusCode.NotFound => "The reservation was not found.",
                    HttpStatusCode.Conflict => "A check is already registered for this reservation.",
                    _ => $"The check could not be saved. API status: {(int)response.StatusCode}."
                };

                ModelState.AddModelError("", mensaje);
                return View(pCheque);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "Unable to connect to the API.");
                return View(pCheque);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "The API took too long to respond. Check whether the check was saved before retrying.");
                return View(pCheque);
            }
        }



        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage reservationResponse = await client.GetAsync($"Reservaciones/BuscarReserva?id={id}");

                if (reservationResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", User.IsInRole("Customer") ? "Clientes" : "Empleados");
                }

                if (reservationResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                if (reservationResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("Reservation not found.");
                }

                if (!reservationResponse.IsSuccessStatusCode)
                {
                    return StatusCode(502, "The reservation could not be loaded.");
                }

                string reservationContent = await reservationResponse.Content.ReadAsStringAsync();
                var reservation = JsonConvert.DeserializeObject<Reservacion>(reservationContent);

                if (reservation == null || reservation.Id != id)
                {
                    return StatusCode(502, "The API returned an invalid reservation.");
                }

                if (reservation.Estado != 'A' || reservation.TipoPago != "Cheque")
                {
                    return BadRequest("The reservation must be active and use check payment.");
                }

                using HttpResponseMessage response = await client.GetAsync($"Cheques/Consultar?Id={id}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", User.IsInRole("Customer") ? "Clientes" : "Empleados");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["NumeroReserva"] = reservation.Id;

                    return RedirectToAction("Create");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(502, "The check could not be loaded.");
                }

                string contenido = await response.Content.ReadAsStringAsync();
                var cheque = JsonConvert.DeserializeObject<Cheque>(contenido);

                if (cheque == null || cheque.IdCheque <= 0 || cheque.IdReservacion != reservation.Id)
                {
                    return StatusCode(502, "The API returned an invalid check.");
                }

                return View(cheque);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, "Unable to connect to the API.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "The API took too long to respond.");
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return StatusCode(502, "The API returned an invalid response.");
            }
        }

        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Cheque pCheque)
        {
            if (pCheque.IdCheque <= 0 || pCheque.IdReservacion <= 0)
            {
                return BadRequest();
            }

            if (pCheque.NumeroCheque <= 0)
            {
                ModelState.AddModelError("NumeroCheque", "Enter a positive check number.");
            }

            if (string.IsNullOrWhiteSpace(pCheque.NombreBanco))
            {
                ModelState.AddModelError("NombreBanco", "Enter the bank name.");
            }

            if (!ModelState.IsValid)
            {
                return View(pCheque);
            }

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await client.PutAsJsonAsync("Cheques/Modificar", pCheque);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", User.IsInRole("Customer") ? "Clientes" : "Empleados");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("The check or its reservation was not found.");
                }

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(User.IsInRole("Customer") ? "ListadoCliente" : "Listado", "Reservaciones");
                }

                string mensaje = "The check changes could not be saved.";

                if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
                {
                    string contenido = await response.Content.ReadAsStringAsync();
                    var error = Newtonsoft.Json.Linq.JObject.Parse(contenido);
                    mensaje = error.Value<string>("message") ?? error.Value<string>("Message") ?? mensaje;
                }

                ModelState.AddModelError("", mensaje);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "Communication with the API failed. Check the saved information before retrying.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "The API took too long to respond. Check the saved information before retrying.");
            }
            catch (Newtonsoft.Json.JsonException)
            {
                ModelState.AddModelError("", "The API returned an unexpected response.");
            }

            return View(pCheque);
        }



        public async Task<int> GetNumCheque()
        {
            int ultimoId = 0;
            List<Cheque> listado = new List<Cheque>();

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            HttpResponseMessage response = await client.GetAsync("/Cheques/Listado");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var resultados = response.Content.ReadAsStringAsync().Result;

                listado = JsonConvert.DeserializeObject<List<Cheque>>(resultados);
            }

            foreach (var item in listado)
            {
                ultimoId = item.IdCheque;
            }

            return ultimoId + 1;
        }



        public async Task<int> GetNumReserva()
        {
            int ultimoId = 0;
            List<Reservacion> listado = new List<Reservacion>();

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            HttpResponseMessage response = await client.GetAsync("/Reservaciones/ListaReservas");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var resultados = response.Content.ReadAsStringAsync().Result;

                listado = JsonConvert.DeserializeObject<List<Reservacion>>(resultados);
            }

            foreach (var item in listado)
            {
                ultimoId = item.Id;
            }

            return ultimoId;
        }

        private AuthenticationHeaderValue AutorizacionToken()
        {
            var token = HttpContext.Session.GetString("token");

            AuthenticationHeaderValue autorizacion = null;

            if (token != null && token.Length != 0)
            {
                autorizacion = new AuthenticationHeaderValue("Bearer", token);
            }//end if

            return autorizacion;
        }//end AutorizacionToken


        private bool ValidarTransaccion(HttpStatusCode resultado)
        {
            if (resultado == HttpStatusCode.Unauthorized)
            {
                TempData["MensajeSesion"] = "Su sesion no es valida o ha expirado";
                return false;
            }
            else
            {
                TempData["MensajeSesion"] = null;
                return true;
            }

        }//end ValidarTransaccion
    }
}

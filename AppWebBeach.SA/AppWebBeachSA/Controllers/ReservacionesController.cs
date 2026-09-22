using AppWebBeachSA.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;

namespace AppWebBeachSA.Controllers
{
    public class ReservacionesController : Controller
    {
        private HotelAPI hotelAPI;

        private HttpClient client;

        public ReservacionesController()
        {
            hotelAPI = new HotelAPI();

            client = hotelAPI.Initial();

        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<IActionResult> Listado()
        {
            return await LoadReservationsAsync("Reservaciones/ListaReservas", "Listado", "Empleados");
        }

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> ListadoCliente()
        {
            return await LoadReservationsAsync("Reservaciones/ListaReservasCliente", "ListadoCliente", "Clientes");
        }

        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpGet]
        public IActionResult ConfirmarCliente()
        {
            TempData.Remove("CedulaCliente");

            if (User.IsInRole("Customer"))
            {
                string cedula = User.FindFirst("Cedula")?.Value;

                if (string.IsNullOrWhiteSpace(cedula))
                {
                    TempData["MensajeSesion"] = "Please sign in again to continue.";
                    return RedirectToAction("Logout", "Clientes");
                }

                TempData["CedulaCliente"] = cedula;

                return RedirectToAction("Create");
            }

            return View();
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarCliente(string pCedula)
        {
            TempData.Remove("CedulaCliente");

            pCedula = pCedula?.Trim();
            ViewData["CustomerId"] = pCedula;

            if (string.IsNullOrWhiteSpace(pCedula))
            {
                ModelState.AddModelError("pCedula", "Enter the customer's identification number.");
                return View();
            }

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await client.GetAsync($"Clientes/Buscar?cedula={Uri.EscapeDataString(pCedula)}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", "Empleados");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.NoContent)
                {
                    ViewData["CustomerNotFound"] = true;
                    return View();
                }

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(string.Empty, "The customer could not be loaded. Please try again.");
                    return View();
                }

                string content = await response.Content.ReadAsStringAsync();
                var customer = JsonConvert.DeserializeObject<Cliente>(content);

                if (customer == null || string.IsNullOrWhiteSpace(customer.Cedula))
                {
                    ViewData["CustomerNotFound"] = true;
                    return View();
                }

                if (customer.Estado != 'A')
                {
                    ModelState.AddModelError(string.Empty, "This customer is inactive and cannot make a new reservation.");
                    return View();
                }

                TempData["CedulaCliente"] = customer.Cedula;

                return View(customer);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Unable to connect to the API. Please try again.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "The API took too long to respond. Please try again.");
            }
            catch (JsonException)
            {
                ModelState.AddModelError(string.Empty, "The API returned customer information that could not be read.");
            }

            return View();
        }

        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            TempData.Remove("MensajeReservacion");
            ViewBag.ListaPaquetes = await GetPaquetes();

            return View(new Reservacion
            {
                CedulaCliente = TempData["CedulaCliente"] as string,
                FechaReserva = DateTime.Today.AddDays(1),
                Duracion = 1
            });
        }

        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind] Reservacion pReservacion)
        {
            if (!ModelState.IsValid)
            {
                string errores = string.Join(
                    " | ",
                    ModelState.Values
                        .SelectMany(value => value.Errors)
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? error.Exception?.Message
                            : error.ErrorMessage)
                );

                TempData["Mensaje"] = $"Validation error: {errores}";
                ViewBag.ListaPaquetes = await GetPaquetes();

                return View(pReservacion);
            }

            pReservacion.Id = 0;
            pReservacion.Estado = 'A';

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                HttpResponseMessage resultado = await client.PostAsJsonAsync<Reservacion>("/Reservaciones/AgregarReserva", pReservacion);

                if (resultado.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";

                    string logoutController = User.IsInRole("Customer") ? "Clientes" : "Empleados";

                    return RedirectToAction("Logout", logoutController);
                }

                if (resultado.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                string contenido = await resultado.Content.ReadAsStringAsync();

                if (resultado.StatusCode == HttpStatusCode.OK)
                {
                    var respuestaReserva = JsonConvert.DeserializeObject<ReservacionCreadaResponse>(contenido);

                    if (respuestaReserva == null || respuestaReserva.ReservationId <= 0)
                    {
                        TempData["Mensaje"] = "The reservation response was invalid.";
                        ViewBag.ListaPaquetes = await GetPaquetes();
                        return View(pReservacion);
                    }

                    pReservacion.Id = respuestaReserva.ReservationId;

                    if (string.Equals(pReservacion.TipoPago, "Cheque", StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["NumeroReserva"] = pReservacion.Id;

                        return RedirectToAction("Create", "Cheques");
                    }

                    return RedirectToAction("Index", "Home");
                }

                TempData["Mensaje"] = "The reservation could not be created.";
                ViewBag.ListaPaquetes = await GetPaquetes();

                return View(pReservacion);
            }
            catch (HttpRequestException)
            {
                TempData["Mensaje"] = "Unable to connect to the API. Please try again.";
                ViewBag.ListaPaquetes = await GetPaquetes();

                return View(pReservacion);
            }
            catch (TaskCanceledException)
            {
                TempData["Mensaje"] = "The API took too long to respond. Please try again.";
                ViewBag.ListaPaquetes = await GetPaquetes();

                return View(pReservacion);
            }
            catch (JsonException)
            {
                TempData["Mensaje"] = "The API returned an invalid reservation response.";
                ViewBag.ListaPaquetes = await GetPaquetes();

                return View(pReservacion);
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
                using HttpResponseMessage response = await client.GetAsync($"Reservaciones/BuscarReserva?id={id}");

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
                    return NotFound();
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(502, "The reservation could not be loaded.");
                }

                string contenido = await response.Content.ReadAsStringAsync();
                var reserva = JsonConvert.DeserializeObject<Reservacion>(contenido);

                if (reserva == null)
                {
                    return StatusCode(502, "The API returned an invalid reservation.");
                }

                ViewBag.ListaPaquetes = await GetPaquetes() ?? new List<Paquete>();
                return View(reserva);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, "Unable to connect to the API.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "The API took too long to respond.");
            }
            catch (JsonException)
            {
                return StatusCode(502, "The API returned an invalid response.");
            }
        }

        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Reservacion pReserva)
        {
            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                if (ModelState.IsValid)
                {
                    using HttpResponseMessage response = await client.PutAsJsonAsync("Reservaciones/Editar", pReserva);

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
                        return NotFound();
                    }

                    string contenido = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var respuesta = Newtonsoft.Json.Linq.JObject.Parse(contenido);
                        bool needsCheck = respuesta.Value<bool?>("needsCheck") ?? respuesta.Value<bool?>("NeedsCheck") ?? false;

                        if (needsCheck)
                        {
                            TempData["NumeroReserva"] = pReserva.Id;
                            return RedirectToAction("Create", "Cheques");
                        }

                        return RedirectToAction(User.IsInRole("Customer") ? "ListadoCliente" : "Listado");
                    }

                    string mensaje = "The reservation changes could not be saved.";

                    if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
                    {
                        var error = Newtonsoft.Json.Linq.JObject.Parse(contenido);
                        mensaje = error.Value<string>("message") ?? error.Value<string>("Message") ?? "Check the reservation information.";
                    }

                    ModelState.AddModelError("", mensaje);
                }
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "Unable to connect to the API.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "The API took too long to respond. Check your reservation before trying again.");
            }
            catch (JsonException)
            {
                ModelState.AddModelError("", "The API returned an unexpected response. Check your reservation before trying again.");
            }

            ViewBag.ListaPaquetes = await GetPaquetes() ?? new List<Paquete>();
            return View(pReserva);
        }



        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return BadRequest();
            }

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await client.GetAsync($"Reservaciones/BuscarReserva?id={id.Value}");

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
                    return NotFound();
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(502, "The reservation could not be loaded.");
                }

                string contenido = await response.Content.ReadAsStringAsync();
                var reserva = JsonConvert.DeserializeObject<Reservacion>(contenido);

                if (reserva == null)
                {
                    return StatusCode(502, "The API returned an invalid reservation.");
                }

                return View(reserva);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, "Unable to connect to the API.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "The API took too long to respond.");
            }
            catch (JsonException)
            {
                return StatusCode(502, "The API returned an invalid response.");
            }
        }

        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await client.DeleteAsync($"Reservaciones/Eliminar?id={id}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", User.IsInRole("Customer") ? "Clientes" : "Empleados");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                {
                    return RedirectToAction(User.IsInRole("Customer") ? "ListadoCliente" : "Listado");
                }

                TempData["DeleteError"] = "The reservation could not be deleted. Please try again.";
            }
            catch (HttpRequestException)
            {
                TempData["DeleteError"] = "Communication with the API failed. Check your reservation list before trying again.";
            }
            catch (TaskCanceledException)
            {
                TempData["DeleteError"] = "The API took too long to respond. Check your reservation list before trying again.";
            }

            return RedirectToAction("Delete", new { id });
        }



        public async Task<List<Paquete>> GetPaquetes()
        {
            List<Paquete> listado = new List<Paquete>();

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            HttpResponseMessage response = await client.GetAsync("/Paquetes/Listado");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var resultados = response.Content.ReadAsStringAsync().Result;

                listado = JsonConvert.DeserializeObject<List<Paquete>>(resultados);
            }

            return listado;
        }


        [Authorize(Roles = "Admin,Employee,Customer")]
        [HttpGet]
        public async Task<IActionResult> DownloadReceipt(int id)
        {
            string returnAction = User.IsInRole("Admin") || User.IsInRole("Employee") ? "Listado" : "ListadoCliente";

            if (id <= 0)
            {
                TempData["ReceiptError"] = "A valid reservation ID is required.";
                return RedirectToAction(returnAction);
            }

            try
            {
                client.DefaultRequestHeaders.Authorization = AutorizacionToken();

                using HttpResponseMessage response = await client.GetAsync($"Reservaciones/DownloadReceipt?id={id}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";

                    string loginController = User.IsInRole("Admin") || User.IsInRole("Employee") ? "Empleados" : "Clientes";

                    return RedirectToAction("Logout", loginController);
                }

                if (!response.IsSuccessStatusCode)
                {
                    string message;

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Forbidden:
                            message = "You do not have permission to download this receipt.";
                            break;

                        case HttpStatusCode.NotFound:
                            message = "The reservation or customer was not found.";
                            break;

                        case HttpStatusCode.Conflict:
                            message = "Register the check details before downloading this receipt.";
                            break;

                        case HttpStatusCode.ServiceUnavailable:
                            message = "The exchange rate is temporarily unavailable. Please try again later.";
                            break;

                        default:
                            message = "The receipt could not be generated. Please try again.";
                            break;
                    }

                    TempData["ReceiptError"] = message;
                    return RedirectToAction(returnAction);
                }

                if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ReceiptError"] = "The service returned an invalid receipt.";
                    return RedirectToAction(returnAction);
                }

                byte[] pdf = await response.Content.ReadAsByteArrayAsync();

                if (pdf.Length == 0)
                {
                    TempData["ReceiptError"] = "The service returned an empty receipt.";
                    return RedirectToAction(returnAction);
                }

                Response.Headers["Cache-Control"] = "no-store";

                return File(pdf, "application/pdf", $"Reservation-{id}.pdf");
            }
            catch (HttpRequestException)
            {
                TempData["ReceiptError"] = "Unable to connect to the service. Please try again later.";
                return RedirectToAction(returnAction);
            }
            catch (TaskCanceledException)
            {
                TempData["ReceiptError"] = "The service took too long to respond. Please try again.";
                return RedirectToAction(returnAction);
            }
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

            return ultimoId + 1;
        }

        private async Task ExtraerTipoCambioAsync()
        {
            ViewData["ExchangeRate"] = null;

            try
            {
                using var exchangeClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                using HttpResponseMessage response = await exchangeClient.GetAsync("https://apis.gometa.org/tdc/tdc.json");

                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

                string result = await response.Content.ReadAsStringAsync();
                var exchangeRate = JsonConvert.DeserializeObject<TipoCambio>(result);

                if (exchangeRate != null && exchangeRate.venta > 0)
                {
                    ViewData["ExchangeRate"] = exchangeRate.venta;
                }
            }
            catch (HttpRequestException)
            {
                ViewData["ExchangeRate"] = null;
            }
            catch (TaskCanceledException)
            {
                ViewData["ExchangeRate"] = null;
            }
            catch (JsonException)
            {
                ViewData["ExchangeRate"] = null;
            }
        }

        private async Task<IActionResult> LoadReservationsAsync(string endpoint, string viewName, string loginController)
        {
            var model = new ReservacionPaqueteLista
            {
                ListaReservaciones = new List<Reservacion>(),
                ListaPaquetes = new List<Paquete>()
            };

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await client.GetAsync(endpoint);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", loginController);
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewData["LoadError"] = "Reservations could not be loaded. Please try again.";
                    return View(viewName, model);
                }

                string content = await response.Content.ReadAsStringAsync();
                var reservations = JsonConvert.DeserializeObject<List<Reservacion>>(content);

                if (reservations == null)
                {
                    ViewData["LoadError"] = "The API returned an invalid reservation list.";
                    return View(viewName, model);
                }

                model.ListaReservaciones = reservations;

                if (reservations.Count > 0)
                {
                    model.ListaPaquetes = await GetPaquetes() ?? new List<Paquete>();
                    await ExtraerTipoCambioAsync();
                }

                return View(viewName, model);
            }
            catch (HttpRequestException)
            {
                ViewData["LoadError"] = "Unable to connect to the API. Please try again.";
            }
            catch (TaskCanceledException)
            {
                ViewData["LoadError"] = "The API took too long to respond. Please try again.";
            }
            catch (JsonException)
            {
                ViewData["LoadError"] = "Some reservation information could not be read.";
            }

            return View(viewName, model);
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

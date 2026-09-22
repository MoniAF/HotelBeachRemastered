using AppWebBeachSA.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Claims;
using static AppWebBeachSA.Controllers.ClientesController;
using AppWebBeachSA.ViewModels;
using System.Net.Http.Json;


namespace AppWebBeachSA.Controllers
{
    public class ClientesController : Controller
    {

        private HotelAPI apiHotel;
        private HttpClient httpClient;
        public static Cliente DatosPersona = new Cliente();


        public ClientesController()
        {
            apiHotel = new HotelAPI();
            httpClient = apiHotel.Initial();
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clientes = new List<Cliente>();

            httpClient.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync("Clientes/Listado");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", "Empleados");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewData["LoadError"] = "The customer list could not be loaded. Please try again.";
                    return View(clientes);
                }

                string contenido = await response.Content.ReadAsStringAsync();
                var resultado = JsonConvert.DeserializeObject<List<Cliente>>(contenido);

                if (resultado == null)
                {
                    ViewData["LoadError"] = "The API returned an invalid customer list.";
                    return View(clientes);
                }

                return View(resultado);
            }
            catch (HttpRequestException)
            {
                ViewData["LoadError"] = "Unable to connect to the API. Please try again.";
            }
            catch (TaskCanceledException)
            {
                ViewData["LoadError"] = "The API took too long to respond. Please try again.";
            }
            catch (Newtonsoft.Json.JsonException)
            {
                ViewData["LoadError"] = "The customer information could not be read.";
            }

            return View(clientes);
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult Create()
        {
            string cedula = TempData["Cedula"] as string;
            string tipoCedula = TempData["TipoCedula"] as string;
            string nombreCompleto = TempData["NombreCompleto"] as string;

            if (string.IsNullOrWhiteSpace(cedula) || string.IsNullOrWhiteSpace(tipoCedula) || string.IsNullOrWhiteSpace(nombreCompleto))
            {
                return RedirectToAction("ObtenerInformacionPersona");
            }

            CustomerRegisterViewModel model = new CustomerRegisterViewModel();
            model.Cedula = cedula;
            model.TipoCedula = tipoCedula;
            model.NombreCompleto = nombreCompleto;

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using HttpResponseMessage response = await httpClient.PostAsJsonAsync("Clientes/CrearCuenta", model);

                    if (response.IsSuccessStatusCode)
                    {
                        if (User.IsInRole("Admin") || User.IsInRole("Employee"))
                        {
                            return RedirectToAction("Index");
                        }

                        TempData["Mensaje"] = "Account created successfully. Sign in with your email and password.";
                        return RedirectToAction("Login");
                    }

                    if (response.StatusCode == HttpStatusCode.Conflict)
                    {
                        ModelState.AddModelError("", "An account already exists with that identification number or email address.");
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        string contenido = await response.Content.ReadAsStringAsync();
                        var error = JObject.Parse(contenido);
                        bool addedErrors = false;

                        if (error["errors"] is JObject validationErrors)
                        {
                            foreach (var property in validationErrors.Properties())
                            {
                                if (property.Value is JArray messages)
                                {
                                    foreach (var message in messages)
                                    {
                                        ModelState.AddModelError("", message.ToString());
                                        addedErrors = true;
                                    }
                                }
                            }
                        }

                        if (!addedErrors)
                        {
                            string mensaje = error.Value<string>("message") ?? error.Value<string>("Message") ?? "Check the registration information and try again.";
                            ModelState.AddModelError("", mensaje);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "The account could not be created. Please try again later.");
                    }
                }
                catch (HttpRequestException)
                {
                    ModelState.AddModelError("", "Communication with the API failed. Try signing in before submitting the registration again.");
                }
                catch (TaskCanceledException)
                {
                    ModelState.AddModelError("", "The API took too long to respond. Try signing in before submitting the registration again.");
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    ModelState.AddModelError("", "The API returned an unexpected response. Check the account before trying again.");
                }
            }

            model.Password = string.Empty;
            model.ConfirmPassword = string.Empty;

            ModelState.Remove(nameof(CustomerRegisterViewModel.Password));
            ModelState.Remove(nameof(CustomerRegisterViewModel.ConfirmPassword));

            ModelState.AddModelError("", "Please enter and confirm the password again before submitting.");

            return View(model);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            httpClient.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync($"Clientes/Buscar?cedula={Uri.EscapeDataString(id.Trim())}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", "Empleados");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("Customer not found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(502, "The customer could not be loaded.");
                }

                string contenido = await response.Content.ReadAsStringAsync();
                var cliente = JsonConvert.DeserializeObject<CustomerEditViewModel>(contenido);

                if (cliente == null || string.IsNullOrWhiteSpace(cliente.Cedula))
                {
                    return StatusCode(502, "The API returned invalid customer information.");
                }

                return View(cliente);
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

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerEditViewModel cliente)
        {
            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            httpClient.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await httpClient.PutAsJsonAsync("Clientes/Modificar", cliente);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", "Empleados");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("Customer not found.");
                }

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError("Email", "That email address is already registered to another customer.");
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    string contenido = await response.Content.ReadAsStringAsync();
                    var error = JObject.Parse(contenido);

                    if (error["errors"] is JObject validationErrors)
                    {
                        foreach (var property in validationErrors.Properties())
                        {
                            if (property.Value is JArray messages)
                            {
                                foreach (var message in messages)
                                {
                                    ModelState.AddModelError("", message.ToString());
                                }
                            }
                        }
                    }

                    ModelState.AddModelError("", "Check the customer information and try again.");
                }
                else
                {
                    ModelState.AddModelError("", "The customer changes could not be saved.");
                }
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

            return View(cliente);
        }



        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            return await LoadCustomerViewAsync(id, "Details");
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            return await LoadCustomerViewAsync(id, "Delete");
        }

        private async Task<IActionResult> LoadCustomerViewAsync(string id, string viewName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            httpClient.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync($"Clientes/Buscar?cedula={Uri.EscapeDataString(id.Trim())}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", "Empleados");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("Customer not found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(502, "The customer information could not be loaded.");
                }

                string contenido = await response.Content.ReadAsStringAsync();
                var cliente = JsonConvert.DeserializeObject<Cliente>(contenido);

                if (cliente == null || string.IsNullOrWhiteSpace(cliente.Cedula))
                {
                    return StatusCode(502, "The API returned invalid customer information.");
                }

                return View(viewName, cliente);
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

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            httpClient.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await httpClient.DeleteAsync($"Clientes/EliminarCliente?vCedula={Uri.EscapeDataString(id.Trim())}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", "Empleados");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                {
                    return RedirectToAction("Index");
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    TempData["CustomerDeleteError"] = "This customer has reservations and cannot be deleted.";
                }
                else
                {
                    TempData["CustomerDeleteError"] = "The customer could not be deleted. Please try again.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["CustomerDeleteError"] = "Communication with the API failed. Check the customer list before retrying.";
            }
            catch (TaskCanceledException)
            {
                TempData["CustomerDeleteError"] = "The API took too long to respond. Check the customer list before retrying.";
            }

            return RedirectToAction("Delete", new { id });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ObtenerInformacionPersona()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ObtenerInformacionPersona(string cedula)
        {
            cedula = cedula?.Trim();

            ViewData["CustomerId"] = cedula;

            TempData.Remove("NombreCompleto");
            TempData.Remove("Cedula");
            TempData.Remove("TipoCedula");
            TempData.Remove("Password");

            if (string.IsNullOrWhiteSpace(cedula) || cedula.Length != 9 || cedula.Any(c => c < '0' || c > '9'))
            {
                ModelState.AddModelError("cedula", "Enter a national identification number containing exactly 9 digits.");
                return View();
            }

            try
            {
                using var lookupClient = new HttpClient();
                lookupClient.Timeout = TimeSpan.FromSeconds(10);

                using HttpResponseMessage response = await lookupClient.GetAsync($"https://apis.gometa.org/cedulas/{Uri.EscapeDataString(cedula)}");

                if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.NoContent)
                {
                    ModelState.AddModelError("", "No person was found with that identification number.");
                    return View();
                }

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "The identification service is currently unavailable. Please try again later.");
                    return View();
                }

                string contenido = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(contenido);
                var results = data["results"] as JArray;

                if (results == null || results.Count == 0 || results[0] is not JObject person)
                {
                    ModelState.AddModelError("", "No person was found with that identification number.");
                    return View();
                }

                string nombreCompleto = data["nombre"]?.ToString().Trim();
                string tipoCedula = person["guess_type"]?.ToString().Trim();
                string cedulaEncontrada = data["cedula"]?.ToString().Trim();

                if (string.IsNullOrWhiteSpace(nombreCompleto) || string.IsNullOrWhiteSpace(tipoCedula) || string.IsNullOrWhiteSpace(cedulaEncontrada))
                {
                    ModelState.AddModelError("", "The identification service returned incomplete information.");
                    return View();
                }

                if (!string.Equals(cedulaEncontrada, cedula, StringComparison.Ordinal))
                {
                    ModelState.AddModelError("", "The returned identification number does not match your search.");
                    return View();
                }

                if (nombreCompleto.Length > 150 || tipoCedula.Length > 20)
                {
                    ModelState.AddModelError("", "The returned information exceeds the supported field lengths.");
                    return View();
                }

                TempData["NombreCompleto"] = nombreCompleto;
                TempData["Cedula"] = cedulaEncontrada;
                TempData["TipoCedula"] = tipoCedula;

                return RedirectToAction("Create");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "Unable to connect to the identification service. Please try again later.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "The identification service took too long to respond. Please try again.");
            }
            catch (Newtonsoft.Json.JsonException)
            {
                ModelState.AddModelError("", "The identification service returned information that could not be read.");
            }

            return View();
        }


        //Métodos autenticación


        //Login
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel cliente)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            try
            {
                HttpResponseMessage response = await httpClient.PostAsJsonAsync("Clientes/AutenticarPW", new { Email = cliente.Email, Password = cliente.Password });

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["Mensaje"] = "Invalid email or password.";
                    return View();
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "Unable to sign in. Please try again.";
                    return View();
                }

                var content = await response.Content.ReadAsStringAsync();
                var authorization = JsonConvert.DeserializeObject<AutorizacionResponse>(content);

                if (authorization == null || !authorization.Resultado || string.IsNullOrWhiteSpace(authorization.Token))
                {
                    TempData["Mensaje"] = "Unable to validate your session.";
                    return View();
                }

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorization.Token);

                HttpResponseMessage customerResponse = await httpClient.GetAsync("Clientes/ClienteLogin");

                if (!customerResponse.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "Unable to load your account information.";
                    return View();
                }

                var customerContent = await customerResponse.Content.ReadAsStringAsync();
                var customer = JsonConvert.DeserializeObject<Cliente>(customerContent);

                if (customer == null || customer.TipoUsuario != 3 || string.IsNullOrWhiteSpace(customer.Cedula))
                {
                    TempData["Mensaje"] = "Your account does not have customer access.";
                    return View();
                }

                if (customer.Restablecer != 1)
                {
                    TempData["Mensaje"] =
                        "Your account requires assistance. Please contact the hotel reception.";

                    return View();
                }

                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, cliente.Email));
                identity.AddClaim(new Claim(ClaimTypes.Role, "Customer"));
                identity.AddClaim(new Claim("TipoUsuario", customer.TipoUsuario.ToString()));
                identity.AddClaim(new Claim("Cedula", customer.Cedula));

                var principal = new ClaimsPrincipal(identity);

                HttpContext.Session.SetString("token", authorization.Token);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Index", "Home");
            }
            catch (HttpRequestException)
            {
                TempData["Mensaje"] = "Unable to connect to the service. Please try again later.";
                return View();
            }
            catch (TaskCanceledException)
            {
                TempData["Mensaje"] = "The service took too long to respond. Please try again.";
                return View();
            }
            catch (JsonException)
            {
                TempData["Mensaje"] = "The service returned an unexpected response.";
                return View();
            }
        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            HttpContext.Session.SetString("token", "");
            return RedirectToAction("Login", "Clientes");
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
                TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                return false;
            }

            TempData.Remove("MensajeSesion");
            return true;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.Equals(model.CurrentPassword, model.NewPassword, StringComparison.Ordinal))
            {
                ModelState.AddModelError("NewPassword", "The new password must be different from the current password.");
                return View(model);
            }

            string token = HttpContext.Session.GetString("token");

            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Mensaje"] = "Your session has expired. Please sign in again.";
                return RedirectToAction("Login", "Clientes");
            }

            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using HttpResponseMessage response = await httpClient.PutAsJsonAsync("Clientes/CambiarPassword", model);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    ModelState.AddModelError("", "Your current password could not be verified, or your session has expired. Check your password or sign in again.");
                    return View(model);
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    ModelState.AddModelError("", "You do not have permission to change this password.");
                    return View(model);
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError("", "Check your password details. The new password must contain between 12 and 128 characters and match its confirmation.");
                    return View(model);
                }

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "The password could not be changed. Please try again.");
                    return View(model);
                }

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Remove("token");

                TempData["Mensaje"] = "Password changed successfully. Please sign in again.";
                return RedirectToAction("Login", "Clientes");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "Unable to confirm the password change. Please try signing in with your new password before submitting again.");
                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "The service took too long to respond. Please try signing in with your new password before submitting again.");
                return View(model);
            }
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult ResetPassword(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("The customer identification number is required.");
            }

            ResetCustomerPasswordViewModel model = new ResetCustomerPasswordViewModel();
            model.Cedula = id.Trim();

            return View(model);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ResetPassword(ResetCustomerPasswordViewModel model)
        {
            if (!model.IdentityVerified)
            {
                ModelState.AddModelError("IdentityVerified", "Confirm that you verified the customer's identity in person.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string token = HttpContext.Session.GetString("token");

            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                return RedirectToAction("Logout", "Empleados");
            }

            httpClient.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await httpClient.PutAsJsonAsync("Clientes/ResetCustomerPassword", model);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                    return RedirectToAction("Logout", "Empleados");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Clientes");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    ModelState.AddModelError("", "Customer not found.");
                    return View(model);
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError("", "The customer account is inactive.");
                    return View(model);
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError("", "Check the customer identification number, password requirements, and identity verification.");
                    return View(model);
                }

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "The password could not be reset. Please try again.");
                    return View(model);
                }

                TempData["PasswordResetSuccess"] = "Password reset successfully. The customer can now sign in with the new password.";
                return RedirectToAction("ResetPassword", new { id = model.Cedula });
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "Unable to confirm the password reset. Ask the customer to try signing in with the new password before submitting again.");
                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "The service took too long to respond. Ask the customer to try signing in with the new password before submitting again.");
                return View(model);
            }
        }
    }
}

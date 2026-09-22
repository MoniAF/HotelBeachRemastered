using AppWebBeachSA.Models;
using AppWebBeachSA.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace AppWebBeachSA.Controllers
{
    [Authorize]
    public class EmpleadosController : Controller
    {
        private readonly HotelAPI hotelApi;
        private readonly HttpClient httpClient;

        public EmpleadosController()
        {
            hotelApi = new HotelAPI();
            httpClient = hotelApi.Initial();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var employees = new List<Empleado>();
            httpClient.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync("Empleados/Listado");

                var authorizationError = HandleAuthorizationError(response.StatusCode);

                if (authorizationError != null)
                {
                    return authorizationError;
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewData["LoadError"] = "The employee list could not be loaded. Please try again.";
                    return View(employees);
                }

                string content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<Empleado>>(content);

                if (result == null)
                {
                    ViewData["LoadError"] = "The service returned an invalid employee list.";
                    return View(employees);
                }

                return View(result);
            }
            catch (HttpRequestException)
            {
                ViewData["LoadError"] = "Unable to connect to the service. Please try again later.";
            }
            catch (TaskCanceledException)
            {
                ViewData["LoadError"] = "The service took too long to respond. Please try again.";
            }
            catch (JsonException)
            {
                ViewData["LoadError"] = "The employee information could not be read.";
            }

            return View(employees);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new EmployeeCreateViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            httpClient.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await httpClient.PostAsJsonAsync("Empleados/Agregar", model);

                var authorizationError = HandleAuthorizationError(response.StatusCode);

                if (authorizationError != null)
                {
                    return authorizationError;
                }

                if (response.IsSuccessStatusCode)
                {
                    TempData["EmployeeSuccess"] = "Employee created successfully.";
                    return RedirectToAction("Index");
                }

                await AddApiErrorsAsync(response, "The employee could not be created. Please try again.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "Communication with the service failed. Check the employee list before submitting again.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "The service took too long to respond. Check the employee list before submitting again.");
            }

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            return await LoadEmployeeViewAsync(id, "Edit");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            httpClient.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await httpClient.PutAsJsonAsync("Empleados/Modificar", model);

                var authorizationError = HandleAuthorizationError(response.StatusCode);

                if (authorizationError != null)
                {
                    return authorizationError;
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("Employee not found.");
                }

                if (response.IsSuccessStatusCode)
                {
                    if (User.FindFirstValue(ClaimTypes.NameIdentifier) == $"employee:{model.ID}")
                    {
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        HttpContext.Session.Remove("token");

                        TempData["Mensaje"] = "Your account was updated. Please sign in again.";
                        return RedirectToAction("Login");
                    }

                    TempData["EmployeeSuccess"] = "Employee updated successfully.";
                    return RedirectToAction("Index");
                }

                await AddApiErrorsAsync(response, "The employee changes could not be saved. Please try again.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "Communication with the service failed. Check the saved information before submitting again.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "The service took too long to respond. Check the saved information before submitting again.");
            }

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            return await LoadEmployeeViewAsync(id, "Details");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            return await LoadEmployeeViewAsync(id, "Delete");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            if (id <= 0)
            {
                return BadRequest("A valid employee ID is required.");
            }

            httpClient.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await httpClient.DeleteAsync($"Empleados/Eliminar?ID={id}");

                var authorizationError = HandleAuthorizationError(response.StatusCode);

                if (authorizationError != null)
                {
                    return authorizationError;
                }

                if (response.IsSuccessStatusCode)
                {
                    TempData["EmployeeSuccess"] = "Employee deleted successfully.";
                    return RedirectToAction("Index");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["Mensaje"] = "The employee account no longer exists.";
                    return RedirectToAction("Index");
                }

                TempData["EmployeeDeleteError"] = await ReadApiMessageAsync(response, "The employee could not be deleted. Please try again.");
            }
            catch (HttpRequestException)
            {
                TempData["EmployeeDeleteError"] = "Communication with the service failed. Check the employee list before trying again.";
            }
            catch (TaskCanceledException)
            {
                TempData["EmployeeDeleteError"] = "The service took too long to respond. Check the employee list before trying again.";
            }

            return RedirectToAction("Delete", new { id });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel empleado)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            try
            {
                using HttpResponseMessage response = await httpClient.PostAsJsonAsync("Empleados/AutenticarPW", new { Email = empleado.Email, Password = empleado.Password });

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

                string content = await response.Content.ReadAsStringAsync();
                var authorization = JsonConvert.DeserializeObject<AutorizacionResponse>(content);

                if (authorization == null || !authorization.Resultado || string.IsNullOrWhiteSpace(authorization.Token))
                {
                    TempData["Mensaje"] = "Unable to validate your session.";
                    return View();
                }

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorization.Token);

                using HttpResponseMessage employeeResponse = await httpClient.GetAsync("Empleados/EmpleadoLogin");

                if (!employeeResponse.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "Unable to load your account information.";
                    return View();
                }

                string employeeContent = await employeeResponse.Content.ReadAsStringAsync();
                var employee = JsonConvert.DeserializeObject<Empleado>(employeeContent);

                if (employee == null || employee.ID <= 0 || employee.Estado != 'A' || (employee.TipoUsuario != 1 && employee.TipoUsuario != 2))
                {
                    TempData["Mensaje"] = "Your account does not have staff access.";
                    return View();
                }

                if (string.IsNullOrWhiteSpace(employee.Email))
                {
                    TempData["Mensaje"] = "The service returned incomplete account information.";
                    return View();
                }

                string role = employee.TipoUsuario == 1 ? "Admin" : "Employee";

                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, $"employee:{employee.ID}"));
                identity.AddClaim(new Claim(ClaimTypes.Name, employee.Email));
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
                identity.AddClaim(new Claim("TipoUsuario", employee.TipoUsuario.ToString()));

                var principal = new ClaimsPrincipal(identity);

                HttpContext.Session.SetString("token", authorization.Token);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Index", "Home");
            }
            catch (HttpRequestException)
            {
                TempData["Mensaje"] = "Unable to connect to the service. Please try again later.";
            }
            catch (TaskCanceledException)
            {
                TempData["Mensaje"] = "The service took too long to respond. Please try again.";
            }
            catch (JsonException)
            {
                TempData["Mensaje"] = "The service returned an unexpected response.";
            }

            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Remove("token");

            return RedirectToAction("Login");
        }

        private async Task<IActionResult> LoadEmployeeViewAsync(int id, string viewName)
        {
            if (id <= 0)
            {
                return BadRequest("A valid employee ID is required.");
            }

            httpClient.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync($"Empleados/Consultar?ID={id}");

                var authorizationError = HandleAuthorizationError(response.StatusCode);

                if (authorizationError != null)
                {
                    return authorizationError;
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("Employee not found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(502, "The employee information could not be loaded.");
                }

                string content = await response.Content.ReadAsStringAsync();
                var employee = JsonConvert.DeserializeObject<Empleado>(content);

                if (employee == null || employee.ID != id)
                {
                    return StatusCode(502, "The service returned invalid employee information.");
                }

                if (viewName == "Edit")
                {
                    EmployeeEditViewModel model = new EmployeeEditViewModel();
                    model.ID = employee.ID;
                    model.NombreCompleto = employee.NombreCompleto;
                    model.Email = employee.Email;
                    model.Estado = employee.Estado;

                    return View(viewName, model);
                }

                return View(viewName, employee);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, "Unable to connect to the service.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "The service took too long to respond.");
            }
            catch (JsonException)
            {
                return StatusCode(502, "The service returned an invalid response.");
            }
        }

        private AuthenticationHeaderValue? AutorizacionToken()
        {
            string? token = HttpContext.Session.GetString("token");

            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            return new AuthenticationHeaderValue("Bearer", token);
        }

        private IActionResult? HandleAuthorizationError(HttpStatusCode statusCode)
        {
            if (statusCode == HttpStatusCode.Unauthorized)
            {
                TempData["MensajeSesion"] = "Your session has expired. Please sign in again.";
                return RedirectToAction("Logout", "Empleados");
            }

            if (statusCode == HttpStatusCode.Forbidden)
            {
                return RedirectToAction("AccessDenied", "Clientes");
            }

            return null;
        }

        private async Task AddApiErrorsAsync(HttpResponseMessage response, string fallbackMessage)
        {
            if (response.StatusCode != HttpStatusCode.BadRequest && response.StatusCode != HttpStatusCode.Conflict)
            {
                ModelState.AddModelError("", fallbackMessage);
                return;
            }

            string content = await response.Content.ReadAsStringAsync();

            try
            {
                var error = JObject.Parse(content);

                if (error["errors"] is JObject validationErrors)
                {
                    bool errorsAdded = false;

                    foreach (var property in validationErrors.Properties())
                    {
                        if (property.Value is JArray messages)
                        {
                            foreach (var message in messages)
                            {
                                ModelState.AddModelError("", message.ToString());
                                errorsAdded = true;
                            }
                        }
                    }

                    if (errorsAdded)
                    {
                        return;
                    }
                }

                string? messageText = error.Value<string>("message") ?? error.Value<string>("Message");
                ModelState.AddModelError("", string.IsNullOrWhiteSpace(messageText) ? fallbackMessage : messageText);
            }
            catch (JsonException)
            {
                ModelState.AddModelError("", fallbackMessage);
            }
        }

        private async Task<string> ReadApiMessageAsync(HttpResponseMessage response, string fallbackMessage)
        {
            if (response.StatusCode != HttpStatusCode.BadRequest && response.StatusCode != HttpStatusCode.Conflict)
            {
                return fallbackMessage;
            }

            string content = await response.Content.ReadAsStringAsync();

            try
            {
                var error = JObject.Parse(content);
                string? message = error.Value<string>("message") ?? error.Value<string>("Message");

                return string.IsNullOrWhiteSpace(message) ? fallbackMessage : message;
            }
            catch (JsonException)
            {
                return fallbackMessage;
            }
        }
    }
}
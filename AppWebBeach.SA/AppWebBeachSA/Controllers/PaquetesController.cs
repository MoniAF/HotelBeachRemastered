using AppWebBeachSA.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace AppWebBeachSA.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class PaquetesController : Controller
    {
        private HotelAPI hotelAPI;
        private HttpClient client;

        public PaquetesController()
        {
            hotelAPI = new HotelAPI();
            client = hotelAPI.Initial();
        }


        //Lista
        public async Task<IActionResult> Index(string buscar)
        {
            var packages = new List<Paquete>();
            string search = buscar?.Trim() ?? string.Empty;
            ViewData["Search"] = search;

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                HttpResponseMessage response = await client.GetAsync("Paquetes/Listado");

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
                    ViewData["LoadError"] = "Unable to load packages. Please try again.";
                    return View(packages);
                }

                var content = await response.Content.ReadAsStringAsync();
                packages = JsonConvert.DeserializeObject<List<Paquete>>(content);

                if (packages == null)
                {
                    ViewData["LoadError"] = "The service returned an unexpected response.";
                    return View(new List<Paquete>());
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    packages = packages.Where(package => (package.NombrePaquete ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                return View(packages.OrderBy(package => package.NombrePaquete).ToList());
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
                ViewData["LoadError"] = "The service returned an unexpected response.";
            }

            return View(new List<Paquete>());
        }

        //Agregar
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Paquete { Estado = 'A' });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind] Paquete paquete)
        {
            if (!ModelState.IsValid)
            {
                return View(paquete);
            }

            paquete.ID = 0;
            paquete.FechaRegistro = DateTime.Now;

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            var resultado = await client.PostAsJsonAsync<Paquete>("Paquetes/Agregar", paquete);

            if (ValidarTransaccion(resultado.StatusCode) == false)
            {
                return RedirectToAction("Logout", "Clientes");
            }

            if (resultado.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", "Paquetes");
            }
            else
            {
                TempData["Mensaje"] = "No se logró registrar el paquete";

                return View(paquete);
            }
        }


        //Editar
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var paquete = new Paquete();

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            HttpResponseMessage response = await client.GetAsync($"Paquetes/Consultar?ID={id}");

            if (ValidarTransaccion(response.StatusCode) == false)
            {
                return RedirectToAction("Logout", "Clientes");
            }

            if (response.IsSuccessStatusCode)
            {
                var resultado = response.Content.ReadAsStringAsync().Result;

                paquete = JsonConvert.DeserializeObject<Paquete>(resultado);
            }

            return View(paquete);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind] Paquete paquete)
        {

            if (!ModelState.IsValid)
            {
                return View(paquete);
            }

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            var resultado = await client.PutAsJsonAsync<Paquete>("Paquetes/Modificar", paquete);

            if (ValidarTransaccion(resultado.StatusCode) == false)
            {
                return RedirectToAction("Logout", "Clientes");
            }

            if (resultado.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Mensaje"] = "Datos incorrectos";

                return View(paquete);
            }
        }

        //Eliminar
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var paquete = new Paquete();

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            HttpResponseMessage mensaje = await client.GetAsync($"/Paquetes/Consultar?ID={id}");

            if (ValidarTransaccion(mensaje.StatusCode) == false)
            {
                return RedirectToAction("Logout", "Clientes");
            }

            if (mensaje.IsSuccessStatusCode)
            {
                var resultado = mensaje.Content.ReadAsStringAsync().Result;

                //conversion json a obj
                paquete = JsonConvert.DeserializeObject<Paquete>(resultado);
            }

            return View(paquete);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeletePaquete(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            try
            {
                using HttpResponseMessage response = await client.DeleteAsync($"Paquetes/Eliminar?ID={id}");

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
                    TempData["Mensaje"] = "The package no longer exists.";
                    return RedirectToAction("Index");
                }

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    TempData["SuccessMessage"] = "Package deleted successfully.";
                    return RedirectToAction("Index");
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    TempData["Mensaje"] = "The package could not be deleted. It may have reservations or have changed during the operation. Refresh its details; if it has reservations, change its status to Inactive instead.";
                    return RedirectToAction("Delete", new { id });
                }

                TempData["Mensaje"] = "The API could not complete the deletion. Refresh the package list to check its current status.";
                return RedirectToAction("Index");
            }
            catch (HttpRequestException)
            {
                TempData["Mensaje"] = "The connection to the API was interrupted. Refresh the package list to check whether the deletion was completed.";
                return RedirectToAction("Index");
            }
            catch (TaskCanceledException)
            {
                TempData["Mensaje"] = "The API took too long to respond. Refresh the package list to check whether the deletion was completed.";
                return RedirectToAction("Index");
            }
        }

        //Detalles
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var paquete = new Paquete();

            client.DefaultRequestHeaders.Authorization = AutorizacionToken();

            HttpResponseMessage respuesta = await client.GetAsync($"/Paquetes/Consultar?ID={id}");

            if (ValidarTransaccion(respuesta.StatusCode) == false)
            {
                return RedirectToAction("Logout", "Clientes");
            }

            if (respuesta.IsSuccessStatusCode)
            {
                var resultado = respuesta.Content.ReadAsStringAsync().Result;

                paquete = JsonConvert.DeserializeObject<Paquete>(resultado);
            }

            return View(paquete);
        }


        private AuthenticationHeaderValue AutorizacionToken()
        {
            var token = HttpContext.Session.GetString("token");

            AuthenticationHeaderValue autorizacion = null;

            if (token != null && token.Length != 0)
            {
                autorizacion = new AuthenticationHeaderValue("Bearer", token);
            }

            return autorizacion;
        }


        private bool ValidarTransaccion(HttpStatusCode resultado)
        {
            //Se vencio el token por ende debe hacer cerrar sesion
            if (resultado == HttpStatusCode.Unauthorized)
            {
                TempData["MensajeSesion"] = "Su sesion ha expirado o no es válida";
                return false;
            }
            else
            {
                TempData["MensajeSesion"] = null;
                return true;
            }
        }
    }
}

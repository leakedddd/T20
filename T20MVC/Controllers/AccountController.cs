using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using T20MVC.Models;

namespace T20MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;

        public AccountController(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        private HttpClient Api() => _httpFactory.CreateClient("T20API");

        public IActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string correo, string clave, string returnUrl)
        {
            using var http = Api();

            var payload = new { correo, clave };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var resp = await http.PostAsync("api/Auth/login", content);

            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Error = "Correo o clave incorrectos.";
                return View();
            }

            var json = await resp.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<LoginResponseDto>(json);

            if (user == null || user.IdCliente <= 0)
            {
                ViewBag.Error = "Respuesta inválida del servidor.";
                return View();
            }

            HttpContext.Session.SetInt32("clienteId", user.IdCliente);
            HttpContext.Session.SetString("clienteNombre", user.Nombre ?? "");

            return Redirect(string.IsNullOrEmpty(returnUrl) ? Url.Action("Index", "T20")! : returnUrl);
        }

        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string nombre, string apellido, string correo, string telefono, string clave)
        {
            using var http = Api();

            var payload = new { nombre, apellido, correo, telefono, clave };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var resp = await http.PostAsync("api/Auth/register", content);

            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Error = "No se pudo crear la cuenta (¿correo ya existe?).";
                return View();
            }

            TempData["ok"] = "Cuenta creada. Ahora inicia sesión.";
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "T20");
        }
    }
}

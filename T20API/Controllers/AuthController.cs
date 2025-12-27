using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using T20API.Models.DTO;
using T20API.Repository.Interfaces;

namespace T20API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ICliente _cliente;

        public AuthController(ICliente cliente)
        {
            _cliente = cliente;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            var user = _cliente.Login(req.Correo, req.Clave);
            if (user == null) return Unauthorized(new { message = "Credenciales inválidas" });
            return Ok(user);
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest req)
        {
            _cliente.Register(req);
            return Ok(new { ok = true });
        }
    }
}

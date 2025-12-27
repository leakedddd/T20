using Microsoft.AspNetCore.Mvc;
using T20API.Repository.Interfaces;

namespace T20API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BoletasController : ControllerBase
    {
        private readonly IBoleta _repo;
        public BoletasController(IBoleta repo) => _repo = repo;

        [HttpGet("{idPedido}")]
        public IActionResult Get(string idPedido)
        {
            if (string.IsNullOrWhiteSpace(idPedido)) return BadRequest("idPedido requerido");

            var bol = _repo.Obtener(idPedido);
            return bol == null ? NotFound() : Ok(bol);
        }
    }
}

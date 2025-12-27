using Microsoft.AspNetCore.Mvc;
using T20API.Models.DTO;
using T20API.Repository.Interfaces;

namespace T20API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController : ControllerBase
    {
        private readonly IPedido _repo;
        public PedidosController(IPedido repo) => _repo = repo;

        [HttpPost]
        public IActionResult Post([FromBody] PedidoCreateRequest req)
        {
            if (req == null) return BadRequest();
            if (req.IdCliente <= 0) return BadRequest("idCliente inválido");
            if (string.IsNullOrWhiteSpace(req.Direccion)) return BadRequest("direccion requerida");
            if (req.Items == null || req.Items.Count == 0) return BadRequest("items requeridos");

            try
            {
                var id = _repo.CrearPedido(req);
                return Ok(new PedidoCreateResponse { idPedido = id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al procesar el pedido", detalle = ex.Message });
            }
        }
    }
}

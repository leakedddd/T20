using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using T20API.Models;
using T20API.Repository.Interfaces;

namespace T20API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly IProducto _repo;

        public ProductosController(IProducto repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public IActionResult Get() => Ok(_repo.Listar());

        [HttpGet("categoria/{idCategoria:int}")]
        public IActionResult GetPorCategoria(int idCategoria) => Ok(_repo.ListarPorCategoria(idCategoria));

        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            var p = _repo.Buscar(id);
            return p == null ? NotFound() : Ok(p);
        }
    }
}

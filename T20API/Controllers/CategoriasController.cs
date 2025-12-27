using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using T20API.Repository.Interfaces;

namespace T20API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriasController : ControllerBase
    {
        private readonly ICategoria _repo;
        public CategoriasController(ICategoria repo) => _repo = repo;

        [HttpGet]
        public IActionResult Get() => Ok(_repo.Listar());
    }
}

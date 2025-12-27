namespace T20MVC.Models
{
    public class ShopVM
    {
        public IEnumerable<CategoriaDto> Categorias { get; set; } = new List<CategoriaDto>();
        public int? CategoriaActivaId { get; set; }
        public IEnumerable<ProductoDto> Productos { get; set; } = new List<ProductoDto>();
    }

}

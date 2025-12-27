using T20API.Models;

namespace T20API.Repository.Interfaces
{
    public interface IProducto
    {
        IEnumerable<Producto> Listar();
        IEnumerable<Producto> ListarPorCategoria(int idCategoria);
        Producto Buscar(int id);
    }
}

using T20API.Models;

namespace T20API.Repository.Interfaces
{
    public interface ICategoria
    {
        IEnumerable<Categoria> Listar();
    }
}

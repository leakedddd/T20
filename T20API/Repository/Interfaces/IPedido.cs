using T20API.Models.DTO;

namespace T20API.Repository.Interfaces
{
    public interface IPedido
    {
        string CrearPedido(PedidoCreateRequest req);
    }
}

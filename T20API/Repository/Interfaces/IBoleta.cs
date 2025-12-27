using T20API.Models.DTO;

namespace T20API.Repository.Interfaces
{
    public interface IBoleta
    {
        BoletaResponse? Obtener(string idPedido);
    }
}

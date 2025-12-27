using T20API.Models.DTO;

namespace T20API.Repository.Interfaces
{
    public interface ICliente
    {
        LoginResponse Login(string correo, string clave);
        void Register(RegisterRequest req);
    }
}

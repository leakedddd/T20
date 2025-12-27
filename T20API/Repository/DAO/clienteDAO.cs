using Microsoft.Data.SqlClient;
using T20API.Models.DTO;
using T20API.Repository.Interfaces;

namespace T20API.Repository.DAO
{
    public class clienteDAO : ICliente
    {
        private readonly string _cn;

        public clienteDAO(IConfiguration config)
        {
            _cn = config.GetConnectionString("sql");
        }

        public LoginResponse Login(string correo, string clave)
        {
            using var cn = new SqlConnection(_cn);
            cn.Open();

            string sql = "SELECT id_cliente, nombre, clave FROM cliente WHERE correo=@c";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@c", correo);

            using var dr = cmd.ExecuteReader();
            if (!dr.Read()) return null;

            var stored = dr.GetString(2);
            if (stored != clave) return null;

            return new LoginResponse
            {
                IdCliente = dr.GetInt32(0),
                Nombre = dr.GetString(1)
            };
        }

        public void Register(RegisterRequest req)
        {
            using var cn = new SqlConnection(_cn);
            cn.Open();

            string sql = @"INSERT INTO cliente(nombre,apellido,telefono,correo,clave)
                       VALUES(@n,@a,@t,@c,@p)";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@n", req.Nombre);
            cmd.Parameters.AddWithValue("@a", req.Apellido);
            cmd.Parameters.AddWithValue("@t", (object)req.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@c", req.Correo);
            cmd.Parameters.AddWithValue("@p", req.Clave);
            cmd.ExecuteNonQuery();
        }        
    }
}

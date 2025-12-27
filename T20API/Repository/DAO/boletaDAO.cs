using Microsoft.Data.SqlClient;
using System.Data;
using T20API.Models.DTO;
using T20API.Repository.Interfaces;

namespace T20API.Repository.DAO
{
    public class boletaDAO : IBoleta
    {
        private readonly string _cn;
        public boletaDAO(IConfiguration config)
        {
            _cn = config.GetConnectionString("sql")!;
        }

        public BoletaResponse? Obtener(string id)
        {
            var result = new BoletaResponse { Id = id };

            using var cn = new SqlConnection(_cn);
            cn.Open();

            // Cabecera
            using (var cmd = new SqlCommand(@"
                SELECT p.id_pedido, p.fecha_pedido, p.direccion,
                       c.nombre, c.apellido, c.correo
                FROM dbo.pedido p
                INNER JOIN dbo.cliente c ON c.id_cliente = p.id_cliente
                WHERE p.id_pedido = @id", cn))
            {
                cmd.Parameters.AddWithValue("@id", id);

                using var rd = cmd.ExecuteReader();
                if (!rd.Read()) return null;

                result.Id = rd.GetString(0);
                result.Fecha = rd.GetDateTime(1);
                result.Direccion = rd.GetString(2);
                result.ClienteNombre = rd.GetString(3);
                result.ClienteApellido = rd.GetString(4);
                result.Correo = rd.GetString(5);
            }

            // Detalle
            using (var cmd = new SqlCommand(@"
                SELECT p.nombre, d.cantidad, d.precio
                FROM dbo.detalle_pedido d
                JOIN dbo.producto p ON p.id_producto = d.id_producto
                WHERE d.id_pedido = @id
                ORDER BY p.nombre", cn))
            {
                cmd.Parameters.AddWithValue("@id", id);

                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    result.Detalle.Add(new BoletaItemResponse
                    {
                        Nombre = rd.GetString(0),
                        Cantidad = rd.GetInt32(1),
                        Precio = rd.GetDecimal(2)
                    });
                }
            }

            return result;
        }
    }
}

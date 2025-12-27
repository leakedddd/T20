using Microsoft.Data.SqlClient;
using System.Data;
using T20API.Models.DTO;
using T20API.Repository.Interfaces;

namespace T20API.Repository.DAO
{
    public class pedidoDAO : IPedido
    {
        private readonly string _cn;

        public pedidoDAO(IConfiguration config)
        {
            _cn = config.GetConnectionString("sql");
        }

        public string CrearPedido(PedidoCreateRequest req)
        {
            using var cn = new SqlConnection(_cn);
            cn.Open();

            using var tr = cn.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                // 1) crear cabecera pedido (SP que ya tienes)
                using var cmd = new SqlCommand("usp_add_pedido", cn, tr);
                cmd.CommandType = CommandType.StoredProcedure;

                var outId = new SqlParameter("@idpedido", SqlDbType.VarChar, 8)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(outId);
                cmd.Parameters.AddWithValue("@idcliente", req.IdCliente);
                cmd.Parameters.AddWithValue("@direccion", req.Direccion);

                cmd.ExecuteNonQuery();
                string idPedido = (string)outId.Value;

                // 2) insertar detalle: precio sale de BD
                foreach (var it in req.Items)
                {
                    if (it.Cantidad < 1) throw new Exception("Cantidad inválida.");

                    // trae precio actual desde BD (bloqueado por la transacción)
                    decimal precio;
                    using (var c2 = new SqlCommand("SELECT precio FROM producto WHERE id_producto=@id", cn, tr))
                    {
                        c2.Parameters.AddWithValue("@id", it.IdProducto);
                        var obj = c2.ExecuteScalar();
                        if (obj == null) throw new Exception($"Producto {it.IdProducto} no existe.");
                        precio = Convert.ToDecimal(obj);
                    }

                    // usa tu SP de detalle
                    using var cd = new SqlCommand("usp_add_pedido_detalle", cn, tr);
                    cd.CommandType = CommandType.StoredProcedure;
                    cd.Parameters.AddWithValue("@ipedido", idPedido);
                    cd.Parameters.AddWithValue("@idproducto", it.IdProducto);
                    cd.Parameters.AddWithValue("@cantidad", it.Cantidad);
                    cd.Parameters.AddWithValue("@precio", precio);

                    cd.ExecuteNonQuery();
                }

                tr.Commit();
                return idPedido;
            }
            catch
            {
                tr.Rollback();
                throw;
            }
        }
    }
}

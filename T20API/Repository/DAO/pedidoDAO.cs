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
                // Validar que el cliente existe
                using (var cmdCliente = new SqlCommand("SELECT COUNT(*) FROM cliente WHERE id_cliente=@id", cn, tr))
                {
                    cmdCliente.Parameters.AddWithValue("@id", req.IdCliente);
                    int clienteExiste = (int)cmdCliente.ExecuteScalar();
                    if (clienteExiste == 0)
                        throw new InvalidOperationException($"El cliente {req.IdCliente} no existe.");
                }

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
                    if (it.Cantidad < 1) throw new InvalidOperationException("Cantidad inválida.");

                    // trae precio actual y stock desde BD (bloqueado por la transacción)
                    decimal precio;
                    int stockDisponible;
                    string nombreProducto;

                    using (var c2 = new SqlCommand("SELECT precio, stock, nombre FROM producto WHERE id_producto=@id", cn, tr))
                    {
                        c2.Parameters.AddWithValue("@id", it.IdProducto);
                        using var reader = c2.ExecuteReader();

                        if (!reader.Read())
                            throw new InvalidOperationException($"El producto {it.IdProducto} no existe.");

                        precio = reader.GetDecimal(0);
                        stockDisponible = reader.GetInt32(1);
                        nombreProducto = reader.GetString(2);
                    }

                    // Validar stock suficiente
                    if (stockDisponible < it.Cantidad)
                        throw new InvalidOperationException($"Stock insuficiente para '{nombreProducto}'. Disponible: {stockDisponible}, solicitado: {it.Cantidad}");

                    // Descontar stock
                    using (var c3 = new SqlCommand("UPDATE producto SET stock = stock - @cantidad WHERE id_producto=@id", cn, tr))
                    {
                        c3.Parameters.AddWithValue("@cantidad", it.Cantidad);
                        c3.Parameters.AddWithValue("@id", it.IdProducto);
                        c3.ExecuteNonQuery();
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

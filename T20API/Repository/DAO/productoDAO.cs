using Microsoft.Data.SqlClient;
using System.Data;
using T20API.Models;
using T20API.Repository.Interfaces;

namespace T20API.Repository.DAO
{
    public class productoDAO : IProducto
    {
        private readonly string _cn;

        public productoDAO(IConfiguration config)
        {
            _cn = config.GetConnectionString("sql");
        }

        public IEnumerable<Producto> Listar()
        {
            var lista = new List<Producto>();
            using var cn = new SqlConnection(_cn);
            cn.Open();

            using var cmd = new SqlCommand("usp_productos", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Producto
                {
                    id_producto = dr.GetInt32(0),
                    nombre = dr.GetString(1),
                    precio = dr.GetDecimal(2),
                    categoria = dr.GetInt32(3),
                    stock = dr.GetInt32(4)
                });
            }
            return lista;
        }

        public IEnumerable<Producto> ListarPorCategoria(int idCategoria)
        {
            var lista = new List<Producto>();
            using var cn = new SqlConnection(_cn);
            cn.Open();

            using var cmd = new SqlCommand("usp_productos_por_categoria", cn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@id_categoria", idCategoria);

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Producto
                {
                    id_producto = dr.GetInt32(0),
                    nombre = dr.GetString(1),
                    precio = dr.GetDecimal(2),
                    categoria = dr.GetInt32(3),
                    stock = dr.GetInt32(4)
                });
            }
            return lista;
        }

        public Producto Buscar(int id) => Listar().FirstOrDefault(x => x.id_producto == id);


    }
}

using Microsoft.Data.SqlClient;
using System.Data;
using T20API.Models;
using T20API.Repository.Interfaces;

namespace T20API.Repository.DAO
{
    public class categoriaDAO : ICategoria
    {
        private readonly string _cn;

        public categoriaDAO(IConfiguration config)
        {
            _cn = config.GetConnectionString("sql");
        }

        public IEnumerable<Categoria> Listar()
        {
            var lista = new List<Categoria>();
            using var cn = new SqlConnection(_cn);
            cn.Open();

            using var cmd = new SqlCommand("usp_categorias", cn);
            cmd.CommandType = CommandType.StoredProcedure;

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Categoria
                {
                    id_categoria = dr.GetInt32(0),
                    nombre = dr.GetString(1)
                });
            }
            return lista;
        }
    }
}

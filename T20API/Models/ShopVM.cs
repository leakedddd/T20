using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace T20API.Models
{
    public class ShopVM
    {
        public IEnumerable<Categoria> Categorias { get; set; }
        public int? CategoriaActivaId { get; set; }
        public IEnumerable<Producto> Productos { get; set; }
    }
}


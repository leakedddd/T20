using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace T20API.Models
{
    public class Producto
    {
        public int id_producto { get; set; }
        public string nombre { get; set; }
        public decimal precio { get; set; }
        public int categoria { get; set; }
        public int stock { get; set; }
    }
}
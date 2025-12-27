using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace T20API.Models
{
    public class Registro
    {
        public int codigo { get; set; }
        public string descripcion { get; set; }
        public int categoria { get; set; }
        public decimal precio { get; set; }
        public int cantidad { get; set; }
        public decimal monto { get { return precio * cantidad; } }
    }
}
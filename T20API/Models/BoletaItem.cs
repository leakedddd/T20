    using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace T20API.Models
{
    public class BoletaItem
    {
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Importe => Precio * Cantidad;
    }
}
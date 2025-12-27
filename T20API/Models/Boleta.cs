using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace T20API.Models
{
    public class Boleta
    {
        public string Id { get; set; }
        public DateTime Fecha { get; set; }
        public string ClienteNombre { get; set; }
        public string ClienteApellido { get; set; }
        public string Correo { get; set; }
        public string Direccion { get; set; }

        public List<BoletaItem> Detalle { get; set; } = new List<BoletaItem>();
        public decimal Total => Detalle.Sum(x => x.Importe);
    }
}
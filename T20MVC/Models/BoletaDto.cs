namespace T20MVC.Models
{
    public class BoletaDto
    {
        public string Id { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string ClienteNombre { get; set; } = "";
        public string ClienteApellido { get; set; } = "";
        public string Correo { get; set; } = "";
        public string Direccion { get; set; } = "";

        public List<BoletaItemDto> Detalle { get; set; } = new();
        public decimal Total => Detalle.Sum(x => x.Importe);
    }

    public class BoletaItemDto
    {
        public string Nombre { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Importe => Precio * Cantidad;
    }
}

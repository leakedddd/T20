namespace T20API.Models.DTO
{
    public class BoletaResponse
    {
        public string Id { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string ClienteNombre { get; set; } = "";
        public string ClienteApellido { get; set; } = "";
        public string Correo { get; set; } = "";
        public string Direccion { get; set; } = "";

        public List<BoletaItemResponse> Detalle { get; set; } = new();
        public decimal Total => Detalle.Sum(x => x.Importe);
    }
}

namespace T20API.Models.DTO
{
    public class BoletaItemResponse
    {
        public string Nombre { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Importe => Precio * Cantidad;
    }
}

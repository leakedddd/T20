namespace T20API.Models.DTO
{
    public class PedidoItemRequest
    {
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
    }
}

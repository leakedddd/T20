namespace T20API.Models.DTO
{
    public class PedidoCreateRequest
    {
        public int IdCliente { get; set; }
        public string Direccion { get; set; }
        public List<PedidoItemRequest> Items { get; set; } = new();
    }
}

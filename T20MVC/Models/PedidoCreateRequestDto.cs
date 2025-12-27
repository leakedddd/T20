namespace T20MVC.Models
{
    public class PedidoCreateRequestDto
    {
        public int idCliente { get; set; }
        public string direccion { get; set; } = ""; 

        public List<PedidoItemRequestDto> items { get; set; }
            = new List<PedidoItemRequestDto>();
    }
}

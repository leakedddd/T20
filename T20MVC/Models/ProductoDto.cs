namespace T20MVC.Models
{
    public class ProductoDto
    {
        public int id_producto { get; set; }
        public string nombre { get; set; }
        public decimal precio { get; set; }
        public int categoria { get; set; }
        public int stock { get; set; }
    }

}

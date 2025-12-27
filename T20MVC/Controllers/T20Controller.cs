using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using T20MVC.Helpers;
using T20MVC.Models;
using Rotativa.AspNetCore; 
using Rotativa.AspNetCore.Options;


namespace T20MVC.Controllers
{
    public class T20Controller : Controller
    {
        private readonly IHttpClientFactory _httpFactory;

        public T20Controller(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        private HttpClient Api() => _httpFactory.CreateClient("T20API");

        private List<RegistroDto> GetCarrito()
            => HttpContext.Session.GetObject<List<RegistroDto>>("carrito") ?? new List<RegistroDto>();

        private void SaveCarrito(List<RegistroDto> carrito)
        {
            HttpContext.Session.SetObject("carrito", carrito);
            HttpContext.Session.SetInt32("cartCount", carrito.Sum(x => x.cantidad));
        }

        public IActionResult Home() => View();

        // /T20/Index?catId=1
        public async Task<IActionResult> Index(int? catId)
        {
            using var http = Api();

            // 1) Categorías
            var catsResp = await http.GetAsync("api/Categorias");
            if (!catsResp.IsSuccessStatusCode)
                return View(new ShopVM()); // o muestra error si quieres

            var catsJson = await catsResp.Content.ReadAsStringAsync();
            var cats = JsonConvert.DeserializeObject<List<CategoriaDto>>(catsJson) ?? new();

            if (!cats.Any())
                return View(new ShopVM());

            // 2) Productos por categoría
            var activaId = catId ?? cats.First().id_categoria;

            var prodsResp = await http.GetAsync($"api/Productos/categoria/{activaId}");
            if (!prodsResp.IsSuccessStatusCode)
                return View(new ShopVM { Categorias = cats, CategoriaActivaId = activaId, Productos = new List<ProductoDto>() });

            var prodsJson = await prodsResp.Content.ReadAsStringAsync();
            var prods = JsonConvert.DeserializeObject<List<ProductoDto>>(prodsJson) ?? new();

            var vm = new ShopVM
            {
                Categorias = cats,
                CategoriaActivaId = activaId,
                Productos = prods
            };

            // inicializa carrito si no existe
            if (HttpContext.Session.GetString("carrito") == null)
                SaveCarrito(new List<RegistroDto>());

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Select(int id, int qty = 1)
        {
            if (qty < 1) qty = 1;

            using var http = Api();

            // Trae producto por id desde API
            var resp = await http.GetAsync($"api/Productos/{id}");
            if (!resp.IsSuccessStatusCode)
                return Json(new { ok = false });

            var json = await resp.Content.ReadAsStringAsync();
            var p = JsonConvert.DeserializeObject<ProductoDto>(json);

            if (p == null) return Json(new { ok = false });

            var carrito = GetCarrito();
            var item = carrito.FirstOrDefault(x => x.codigo == id);

            if (item != null) item.cantidad += qty;
            else
            {
                carrito.Add(new RegistroDto
                {
                    codigo = p.id_producto,
                    descripcion = p.nombre,
                    categoria = p.categoria,
                    precio = p.precio,
                    cantidad = qty
                });
            }

            SaveCarrito(carrito);
            return Json(new { ok = true, count = HttpContext.Session.GetInt32("cartCount") ?? 0 });
        }

        public IActionResult Carrito()
        {
            var carrito = GetCarrito();
            if (!carrito.Any()) return RedirectToAction("Index");
            return View(carrito);
        }

        public IActionResult Delete(int id)
        {
            var carrito = GetCarrito();
            var item = carrito.FirstOrDefault(x => x.codigo == id);
            if (item != null) carrito.Remove(item);
            SaveCarrito(carrito);
            return RedirectToAction("Carrito");
        }

        [HttpPost]
        public IActionResult UpdateQty(int id, int qty)
        {
            if (qty < 1) qty = 1;

            var carrito = GetCarrito();
            var item = carrito.FirstOrDefault(x => x.codigo == id);
            if (item == null) return Json(new { ok = false });

            item.cantidad = qty;
            SaveCarrito(carrito);

            return Json(new { ok = true, count = HttpContext.Session.GetInt32("cartCount") ?? 0 });
        }

        public IActionResult Pedido()
        {
            var clienteId = HttpContext.Session.GetInt32("clienteId");
            if (clienteId == null)
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Pedido", "T20") });

            var carrito = GetCarrito();
            if (!carrito.Any()) return RedirectToAction("Index");

            ViewBag.carrito = carrito;
            return View(new PedidoVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pedido(PedidoVM vm)
        {
            var clienteId = HttpContext.Session.GetInt32("clienteId");
            if (clienteId == null)
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Pedido", "T20") });

            var carrito = GetCarrito();
            if (!carrito.Any()) return RedirectToAction("Index");

            using var http = Api();

            var payload = new PedidoCreateRequestDto
            {
                idCliente = clienteId.Value,
                direccion = vm.direccion,
                items = carrito.Select(x => new PedidoItemRequestDto
                {
                    idProducto = x.codigo,
                    cantidad = x.cantidad
                }).ToList()
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var resp = await http.PostAsync("api/Pedidos", content);

            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Error = "No se pudo registrar el pedido.";
                ViewBag.carrito = carrito;
                return View(vm);
            }

            var resultJson = await resp.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PedidoCreateResponseDto>(resultJson);

            // limpia carrito
            SaveCarrito(new List<RegistroDto>());

            // mejor pasar por ruta para no depender de TempData
            return RedirectToAction("Mensaje", new { idPedido = result?.idPedido });
        }

        public IActionResult Mensaje(string idPedido)
        {
            ViewBag.idpedido = idPedido;
            return View();
        }

        public async Task<IActionResult> Boleta(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction("Index");

            using var http = Api(); // tu método Api() que crea client a la API

            var resp = await http.GetAsync($"api/Boletas/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var json = await resp.Content.ReadAsStringAsync();
            var bol = JsonConvert.DeserializeObject<BoletaDto>(json);
            if (bol == null)
                return RedirectToAction("Index");

            // PDF desde la vista "Boleta.cshtml"
            return new ViewAsPdf("Boleta", bol)
            {
                FileName = $"Boleta_{id}.pdf",
                PageSize = Size.A4,
                PageMargins = new Margins { Top = 15, Right = 15, Bottom = 15, Left = 15 }
            };
        }

        public IActionResult Help()
        {
            var model = new List<FaqDto>
    {
        new FaqDto { Question = "¿Cuánto tarda en llegar mi pedido?", Answer = "El tiempo de entrega varía según tu ubicación, pero generalmente tarda entre 3 y 7 días hábiles." },
        new FaqDto { Question = "¿Cuáles son los métodos de pago disponibles?", Answer = "Aceptamos pagos con tarjetas de crédito, débito y transferencias bancarias." },
        new FaqDto { Question = "¿Puedo cambiar o devolver un producto?", Answer = "Sí, puedes solicitar un cambio o devolución dentro de los 7 días posteriores a la recepción del pedido, siempre que el producto esté en perfecto estado." },
        new FaqDto { Question = "¿El envío es gratuito?", Answer = "Ofrecemos envío gratuito para clientes que crean una cuenta en nuestra tienda." },
        new FaqDto { Question = "¿Ofrecen cupones de descuento?", Answer = "Por el momento no contamos con tal beneficio pero estamos trabajando en ello." },
        new FaqDto { Question = "¿Cómo puedo contactar con atención al cliente?", Answer = "Puedes escribirnos a support@t20store.com o por nuestras redes sociales." },
    };

            return View(model);
        }



    }
}

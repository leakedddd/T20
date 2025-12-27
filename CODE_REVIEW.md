# Revisión de Código - T20 E-Commerce

## Resumen Ejecutivo

Este documento presenta una revisión completa del código del proyecto T20, una aplicación de e-commerce desarrollada en .NET 8 con arquitectura API + MVC.

**Estado General**: ⚠️ CRÍTICO - Se encontraron vulnerabilidades de seguridad graves que deben abordarse inmediatamente.

---

## 🚨 PROBLEMAS CRÍTICOS DE SEGURIDAD

### 1. Contraseñas en Texto Plano (CRÍTICO)
**Ubicación**: `T20API/Repository/DAO/clienteDAO.cs:28-29, 50`

**Problema**:
```csharp
// Login - línea 28-29
var stored = dr.GetString(2);
if (stored != clave) return null;

// Register - línea 50
cmd.Parameters.AddWithValue("@p", req.Clave);
```

Las contraseñas se almacenan y comparan en texto plano sin ningún tipo de hash o encriptación.

**Impacto**:
- Violación de OWASP A02:2021 - Cryptographic Failures
- Si la base de datos es comprometida, todas las contraseñas quedan expuestas
- Los administradores pueden ver contraseñas de usuarios
- Incumplimiento de GDPR y otras regulaciones de privacidad

**Solución Recomendada**:
```csharp
// Usar BCrypt o Argon2 para hashear contraseñas
// En Register:
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(req.Clave);
cmd.Parameters.AddWithValue("@p", hashedPassword);

// En Login:
string storedHash = dr.GetString(2);
if (!BCrypt.Net.BCrypt.Verify(clave, storedHash)) return null;
```

---

### 2. Sin Autenticación en la API (CRÍTICO)
**Ubicación**: `T20API/Program.cs:32`

**Problema**:
```csharp
app.UseAuthorization(); // ⚠️ Se llama pero no hay autenticación configurada
```

No hay ningún esquema de autenticación configurado (JWT, API Keys, etc.). Cualquiera puede llamar directamente a los endpoints de la API sin credenciales.

**Impacto**:
- Cualquier usuario puede crear pedidos para otros usuarios cambiando el `idCliente`
- Sin rate limiting, vulnerable a ataques DDoS
- No hay auditoría de quién realiza las acciones

**Solución Recomendada**:
Implementar JWT tokens:
```csharp
// En Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
        };
    });

// En los controladores
[Authorize]
[HttpPost]
public IActionResult Post([FromBody] PedidoCreateRequest req) { ... }
```

---

### 3. Falta de Validación de Propietario de Pedidos (CRÍTICO)
**Ubicación**: `T20API/Controllers/PedidosController.cs:22`

**Problema**:
```csharp
[HttpPost]
public IActionResult Post([FromBody] PedidoCreateRequest req)
{
    if (req.IdCliente <= 0) return BadRequest("idCliente inválido");
    // ⚠️ No se valida que el usuario autenticado sea el dueño del IdCliente
    var id = _repo.CrearPedido(req);
    return Ok(new PedidoCreateResponse { idPedido = id });
}
```

Un usuario puede crear pedidos para otros usuarios simplemente cambiando el `idCliente` en el JSON.

**Impacto**:
- Suplantación de identidad
- Fraude
- Creación de pedidos falsos

**Solución Recomendada**:
```csharp
[Authorize]
[HttpPost]
public IActionResult Post([FromBody] PedidoCreateRequest req)
{
    var authenticatedUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

    if (req.IdCliente != authenticatedUserId)
        return Forbid("No puedes crear pedidos para otros usuarios");

    // ... resto del código
}
```

---

### 4. Sin Protección CSRF en API (ALTO)
**Ubicación**: Todos los controladores de API

**Problema**: La API no tiene protección contra ataques CSRF. Aunque el MVC usa `[ValidateAntiForgeryToken]`, la API está desprotegida.

**Solución Recomendada**:
- Usar tokens JWT (elimina la necesidad de CSRF en API)
- O implementar validación de origen (CORS estricto + verificación de headers)

---

### 5. Fuga de Información en Mensajes de Error (MEDIO)
**Ubicación**: `T20API/Controllers/AuthController.cs:23`

**Problema**:
```csharp
if (user == null) return Unauthorized(new { message = "Credenciales inválidas" });
```

Aunque el mensaje es genérico, no hay protección contra ataques de enumeración de usuarios.

**Solución Recomendada**:
- Implementar rate limiting
- Usar tiempos de respuesta constantes
- Considerar captcha después de varios intentos fallidos

---

### 6. Sin Validación de Email Único (MEDIO)
**Ubicación**: `T20API/Repository/DAO/clienteDAO.cs:38-52`

**Problema**: No hay validación de que el correo no exista antes de insertar. Esto puede causar excepción SQL si hay una constraint UNIQUE, pero no se maneja adecuadamente.

**Solución Recomendada**:
```csharp
public void Register(RegisterRequest req)
{
    using var cn = new SqlConnection(_cn);
    cn.Open();

    // Verificar si el correo ya existe
    string checkSql = "SELECT COUNT(*) FROM cliente WHERE correo=@c";
    using var checkCmd = new SqlCommand(checkSql, cn);
    checkCmd.Parameters.AddWithValue("@c", req.Correo);

    int count = (int)checkCmd.ExecuteScalar();
    if (count > 0)
        throw new InvalidOperationException("El correo ya está registrado");

    // ... resto del código de inserción
}
```

---

## ⚠️ PROBLEMAS DE CALIDAD Y RENDIMIENTO

### 7. Consulta Ineficiente en Búsqueda de Productos (ALTO)
**Ubicación**: `T20API/Repository/DAO/productoDAO.cs:66`

**Problema**:
```csharp
public Producto Buscar(int id) => Listar().FirstOrDefault(x => x.id_producto == id);
```

Carga TODOS los productos de la base de datos solo para buscar uno.

**Impacto**:
- Uso excesivo de memoria
- Tiempo de respuesta lento
- Carga innecesaria en la base de datos

**Solución Recomendada**:
```csharp
public Producto Buscar(int id)
{
    using var cn = new SqlConnection(_cn);
    cn.Open();

    using var cmd = new SqlCommand("usp_producto_por_id", cn);
    cmd.CommandType = CommandType.StoredProcedure;
    cmd.Parameters.AddWithValue("@id", id);

    using var dr = cmd.ExecuteReader();
    if (!dr.Read()) return null;

    return new Producto
    {
        id_producto = dr.GetInt32(0),
        nombre = dr.GetString(1),
        precio = dr.GetDecimal(2),
        categoria = dr.GetInt32(3),
        stock = dr.GetInt32(4)
    };
}
```

---

### 8. Falta de Manejo de Errores (ALTO)
**Ubicación**: Todos los controladores

**Problema**: No hay bloques try-catch en los controladores. Las excepciones SQL se propagan directamente al cliente.

**Ejemplo**:
```csharp
[HttpPost("register")]
public IActionResult Register([FromBody] RegisterRequest req)
{
    _cliente.Register(req); // ⚠️ Si falla, el error SQL se expone
    return Ok(new { ok = true });
}
```

**Solución Recomendada**:
```csharp
[HttpPost("register")]
public IActionResult Register([FromBody] RegisterRequest req)
{
    try
    {
        _cliente.Register(req);
        return Ok(new { ok = true });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        // Log el error
        _logger.LogError(ex, "Error al registrar usuario");
        return StatusCode(500, new { message = "Error interno del servidor" });
    }
}
```

---

### 9. Sin Logging (ALTO)
**Ubicación**: Todo el proyecto

**Problema**: No hay registro de eventos, errores, o actividad de usuarios.

**Impacto**:
- Dificulta el debugging
- No hay auditoría
- Imposible detectar patrones de ataque
- No se puede rastrear problemas en producción

**Solución Recomendada**:
```csharp
// En Program.cs
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.AddEventLog(); // Para producción en Windows
});

// O usar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// En los controladores
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ICliente cliente, ILogger<AuthController> logger)
    {
        _cliente = cliente;
        _logger = logger;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        _logger.LogInformation("Intento de login para {Email}", req.Correo);
        var user = _cliente.Login(req.Correo, req.Clave);

        if (user == null)
        {
            _logger.LogWarning("Login fallido para {Email}", req.Correo);
            return Unauthorized(new { message = "Credenciales inválidas" });
        }

        _logger.LogInformation("Login exitoso para usuario {UserId}", user.IdCliente);
        return Ok(user);
    }
}
```

---

### 10. Sin Validación de Stock (MEDIO)
**Ubicación**: `T20MVC/Controllers/T20Controller.cs:74-109`, `T20API/Repository/DAO/pedidoDAO.cs:43-66`

**Problema**:
- Se pueden agregar productos al carrito sin verificar stock
- Al crear el pedido tampoco se valida ni se reduce el stock

**Solución Recomendada**:
```csharp
// En pedidoDAO.cs
foreach (var it in req.Items)
{
    if (it.Cantidad < 1) throw new Exception("Cantidad inválida.");

    decimal precio;
    int stockDisponible;

    using (var c2 = new SqlCommand(
        "SELECT precio, stock FROM producto WHERE id_producto=@id", cn, tr))
    {
        c2.Parameters.AddWithValue("@id", it.IdProducto);
        using var reader = c2.ExecuteReader();

        if (!reader.Read())
            throw new Exception($"Producto {it.IdProducto} no existe.");

        precio = reader.GetDecimal(0);
        stockDisponible = reader.GetInt32(1);
    }

    // Validar stock
    if (stockDisponible < it.Cantidad)
        throw new Exception($"Stock insuficiente para {it.IdProducto}");

    // Reducir stock
    using (var c3 = new SqlCommand(
        "UPDATE producto SET stock = stock - @qty WHERE id_producto=@id", cn, tr))
    {
        c3.Parameters.AddWithValue("@qty", it.Cantidad);
        c3.Parameters.AddWithValue("@id", it.IdProducto);
        c3.ExecuteNonQuery();
    }

    // ... insertar detalle
}
```

---

### 11. Magic Strings en Session Keys (MEDIO)
**Ubicación**: `T20MVC/Controllers/T20Controller.cs` y `AccountController.cs`

**Problema**:
```csharp
HttpContext.Session.GetInt32("clienteId")
HttpContext.Session.SetString("clienteNombre", ...)
HttpContext.Session.SetObject("carrito", ...)
HttpContext.Session.SetInt32("cartCount", ...)
```

**Solución Recomendada**:
```csharp
public static class SessionKeys
{
    public const string ClienteId = "clienteId";
    public const string ClienteNombre = "clienteNombre";
    public const string Carrito = "carrito";
    public const string CartCount = "cartCount";
}

// Uso
HttpContext.Session.GetInt32(SessionKeys.ClienteId)
```

---

### 12. FAQs Hardcodeadas (BAJO)
**Ubicación**: `T20MVC/Controllers/T20Controller.cs:232-240`

**Problema**:
```csharp
var model = new List<FaqDto>
{
    new FaqDto { Question = "¿Cuánto tarda...", Answer = "..." },
    // ... 6 FAQs hardcodeadas
};
```

**Solución Recomendada**:
- Mover a base de datos para permitir administración dinámica
- O al menos a un archivo de configuración JSON

---

### 13. Sin Paginación (MEDIO)
**Ubicación**: `T20API/Repository/DAO/productoDAO.cs:17`

**Problema**: El método `Listar()` devuelve todos los productos. Con miles de productos esto causará problemas de rendimiento.

**Solución Recomendada**:
```csharp
public IEnumerable<Producto> Listar(int pageNumber = 1, int pageSize = 20)
{
    var lista = new List<Producto>();
    using var cn = new SqlConnection(_cn);
    cn.Open();

    using var cmd = new SqlCommand("usp_productos_paginado", cn);
    cmd.CommandType = CommandType.StoredProcedure;
    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
    cmd.Parameters.AddWithValue("@PageSize", pageSize);

    // ... resto del código
}
```

---

### 14. Sin Validación de Modelos (MEDIO)
**Ubicación**: Todos los modelos DTO

**Problema**: No hay atributos de validación en los DTOs.

**Solución Recomendada**:
```csharp
public class RegisterRequest
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; }

    [Required(ErrorMessage = "El apellido es requerido")]
    [StringLength(100, MinimumLength = 2)]
    public string Apellido { get; set; }

    [Required(ErrorMessage = "El correo es requerido")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido")]
    public string Correo { get; set; }

    [Phone(ErrorMessage = "Formato de teléfono inválido")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "La contraseña debe contener mayúsculas, minúsculas y números")]
    public string Clave { get; set; }
}

// En los controladores
[HttpPost("register")]
public IActionResult Register([FromBody] RegisterRequest req)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // ... resto del código
}
```

---

### 15. Sin Configuración de CORS Explícita (MEDIO)
**Ubicación**: `T20API/Program.cs`

**Problema**: No hay configuración de CORS, podría causar problemas si se accede desde diferentes dominios.

**Solución Recomendada**:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMVC",
        policy =>
        {
            policy.WithOrigins("https://localhost:7001") // URL del MVC
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// ...

app.UseCors("AllowMVC");
```

---

## 🔄 MEJORAS DE ARQUITECTURA

### 16. Falta de Capa de Servicios (MEDIO)

**Problema**: La lógica de negocio está mezclada entre controladores y DAOs.

**Solución Recomendada**:
Crear una capa de servicios:
```
T20API/
  ├── Controllers/     (solo manejo de HTTP)
  ├── Services/        (lógica de negocio) ← NUEVO
  ├── Repository/      (acceso a datos)
  └── Models/
```

---

### 17. Sin Inyección de Dependencias para Configuration (BAJO)

**Problema**: Los DAOs reciben `IConfiguration` completo cuando solo necesitan connection string.

**Solución Recomendada**:
```csharp
// Crear opciones
public class DatabaseOptions
{
    public string ConnectionString { get; set; }
}

// En Program.cs
builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection("ConnectionStrings"));

// En los DAOs
public class productoDAO : IProducto
{
    private readonly string _cn;

    public productoDAO(IOptions<DatabaseOptions> options)
    {
        _cn = options.Value.ConnectionString;
    }
}
```

---

### 18. Sin Health Checks (BAJO)

**Solución Recomendada**:
```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("sql"));

app.MapHealthChecks("/health");
```

---

## 📊 RESUMEN DE PRIORIDADES

| Prioridad | Problema | Esfuerzo | Impacto |
|-----------|----------|----------|---------|
| 🔴 **CRÍTICO** | Contraseñas en texto plano | Medio | Muy Alto |
| 🔴 **CRÍTICO** | Sin autenticación en API | Alto | Muy Alto |
| 🔴 **CRÍTICO** | Validación de propietario de pedidos | Bajo | Muy Alto |
| 🟠 **ALTO** | Falta de manejo de errores | Medio | Alto |
| 🟠 **ALTO** | Sin logging | Bajo | Alto |
| 🟠 **ALTO** | Consulta ineficiente Buscar() | Bajo | Medio |
| 🟠 **ALTO** | Sin protección CSRF en API | Medio | Alto |
| 🟡 **MEDIO** | Sin validación de stock | Medio | Alto |
| 🟡 **MEDIO** | Sin validación de modelos | Bajo | Medio |
| 🟡 **MEDIO** | Sin paginación | Medio | Medio |
| 🟡 **MEDIO** | Magic strings en session | Bajo | Bajo |
| 🟡 **MEDIO** | Sin CORS explícito | Bajo | Bajo |
| 🔵 **BAJO** | FAQs hardcodeadas | Bajo | Bajo |
| 🔵 **BAJO** | Sin health checks | Bajo | Bajo |

---

## 🛠️ PLAN DE ACCIÓN RECOMENDADO

### Fase 1: Seguridad Crítica (Sprint 1)
1. ✅ Implementar hashing de contraseñas con BCrypt
2. ✅ Implementar autenticación JWT en la API
3. ✅ Agregar validación de propietario en pedidos
4. ✅ Implementar manejo de errores global

### Fase 2: Estabilidad (Sprint 2)
5. ✅ Agregar logging con Serilog
6. ✅ Corregir consulta Buscar() ineficiente
7. ✅ Implementar validación de stock
8. ✅ Agregar validación de modelos con Data Annotations

### Fase 3: Mejoras (Sprint 3)
9. ✅ Implementar paginación
10. ✅ Configurar CORS apropiadamente
11. ✅ Refactorizar magic strings a constantes
12. ✅ Agregar health checks

### Fase 4: Arquitectura (Sprint 4)
13. ✅ Crear capa de servicios
14. ✅ Mover FAQs a base de datos
15. ✅ Implementar rate limiting
16. ✅ Agregar caché para productos

---

## 📝 NOTAS ADICIONALES

### Aspectos Positivos
- ✅ Uso correcto de parámetros SQL (previene SQL injection básico)
- ✅ Uso de transacciones en creación de pedidos
- ✅ Separación de concerns con arquitectura API + MVC
- ✅ Uso de stored procedures
- ✅ Dependency injection correctamente configurado
- ✅ Anti-forgery tokens en MVC

### Deuda Técnica
- Migrar de ADO.NET a Entity Framework Core para mejor mantenibilidad
- Considerar implementar CQRS para separar lecturas de escrituras
- Agregar pruebas unitarias e integración (actualmente no existen)
- Implementar caché distribuido (Redis) para mejor escalabilidad
- Considerar Event Sourcing para auditoría completa

---

## 🔗 RECURSOS RECOMENDADOS

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [JWT Authentication in ASP.NET Core](https://jwt.io/introduction)
- [BCrypt.Net](https://github.com/BcryptNet/bcrypt.net)
- [Serilog](https://serilog.net/)

---

**Fecha de Revisión**: 2025-12-27
**Revisor**: Claude Code
**Versión**: 1.0

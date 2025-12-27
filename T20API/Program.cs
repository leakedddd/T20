using T20API.Repository.DAO;
using T20API.Repository.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IProducto, productoDAO>();
builder.Services.AddScoped<ICliente, clienteDAO>();
builder.Services.AddScoped<ICategoria, categoriaDAO>();
builder.Services.AddScoped<IPedido, pedidoDAO>();
builder.Services.AddScoped<IBoleta, boletaDAO>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

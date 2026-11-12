using EasyShop.Data;
using EasyShop.Models;
using EasyShop.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5080");
var dataDirectory = Path.GetFullPath(builder.Configuration["DataDirectory"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyShop", "data"));
Directory.CreateDirectory(dataDirectory);
builder.Services.AddSingleton<IProductRepository>(_ => new ProductRepository(Path.Combine(dataDirectory, "products.db")));
builder.Services.AddSingleton<ICustomerRepository>(_ => new CustomerRepository(Path.Combine(dataDirectory, "customers.sqlite")));
builder.Services.AddSingleton<ProductService>();
builder.Services.AddSingleton<CustomerService>();
var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self'; script-src 'self'; frame-ancestors 'none'";
    if (context.Request.Method == "POST" && context.Request.Headers.Origin.Count > 0 && context.Request.Headers.Origin != $"http://{context.Request.Host}")
    { context.Response.StatusCode = 403; return; }
    try { await next(); }
    catch (ArgumentException ex) { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { error = ex.Message }); }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Falha ao processar operação");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "Não foi possível acessar os dados. Tente novamente e verifique o terminal." });
    }
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/products", (ProductService service) => service.List());
app.MapPost("/api/products", (Product input, ProductService service) => Results.Created("/api/products", service.Add(input)));
app.MapGet("/api/customers", (CustomerService service) => service.ListAsync());
app.MapPost("/api/customers", async (CustomerInput input, CustomerService service) => Results.Created("/api/customers", await service.AddAsync(input)));
app.Run();

using System.Net.Mail;
using EasyShop.Data;
using EasyShop.Models;

namespace EasyShop.Services;

public sealed class ProductService(IProductRepository repository)
{
    public Product[] List() => repository.List();
    public Product Add(Product product)
    {
        var name = product.Name?.Trim() ?? "";
        if (name.Length is < 2 or > 120) throw new ArgumentException("Informe um nome de produto entre 2 e 120 caracteres.");
        if (product.Price <= 0 || product.Price > 999999999.99m || decimal.Round(product.Price, 2) != product.Price)
            throw new ArgumentException("Informe um preço positivo, até R$ 999.999.999,99, com no máximo duas casas decimais.");
        var categories = product.Categories ?? [];
        var attributes = product.Attributes ?? new();
        if (categories.Length > 20 || categories.Any(c => c is null || c.Length > 60) || attributes.Count > 20 || attributes.Any(a => a.Key.Length > 60 || a.Value is null || a.Value.Length > 200))
            throw new ArgumentException("Use até 20 categorias/atributos, categorias e chaves de até 60 caracteres e valores de até 200.");
        return repository.Add(new Product { Name = name, Price = product.Price, Categories = categories.Select(c => c.Trim()).Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), Attributes = attributes });
    }
}
public sealed class CustomerService(ICustomerRepository repository)
{
    public Task<Customer[]> ListAsync() => repository.ListAsync();
    public Task<Customer> AddAsync(CustomerInput input)
    {
        var name = string.Join(' ', (input.FullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (name.Length is < 3 or > 160 || name.Split(' ').Length < 2) throw new ArgumentException("Informe nome e sobrenome, até 160 caracteres.");
        var email = (input.Email ?? "").Trim().ToLowerInvariant();
        if (email.Length > 254 || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email || !parsed.Host.Contains('.'))
            throw new ArgumentException("Informe um e-mail válido, como nome@exemplo.com.");
        return repository.AddAsync(name, email);
    }
}

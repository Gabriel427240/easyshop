using EasyShop.Data;
using EasyShop.Models;
using EasyShop.Services;
using Xunit;

namespace EasyShop.Tests;

public sealed class BusinessTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "easyshop-tests-" + Guid.NewGuid());
    public BusinessTests() => Directory.CreateDirectory(directory);
    [Theory]
    [InlineData("", 10)]
    [InlineData("Caderno", 0)]
    [InlineData("Caderno", -10)]
    [InlineData("Caderno", 1.234)]
    public void RejectsInvalidProducts(string name, double price)
    {
        using var repository = new ProductRepository(Path.Combine(directory, "products.db"));
        Assert.Throws<ArgumentException>(() => new ProductService(repository).Add(new Product { Name = name, Price = (decimal)price }));
        Assert.Empty(repository.List());
    }
    [Fact]
    public void ProductPersistsAfterReopening()
    {
        var path = Path.Combine(directory, "products.db");
        using (var repository = new ProductRepository(path))
        {
            var saved = new ProductService(repository).Add(new Product { Name = " Caderno ", Price = 29.90m, Categories = ["Papelaria", "papelaria"], Attributes = new() { { "Cor", "Verde" } } });
            Assert.True(saved.Id > 0);
        }
        using var reopened = new ProductRepository(path);
        var result = Assert.Single(reopened.List());
        Assert.Equal("Caderno", result.Name); Assert.Equal(29.90m, result.Price); Assert.Single(result.Categories); Assert.Equal("Verde", result.Attributes["Cor"]);
    }
    [Theory]
    [InlineData("Marina", "marina@example.com")]
    [InlineData("Marina Exemplo", "inválido")]
    [InlineData("Marina Exemplo", "Nome <marina@example.com>")]
    [InlineData("", "")]
    public async Task RejectsInvalidCustomers(string name, string email)
    {
        var repository = new CustomerRepository(Path.Combine(directory, "customers.sqlite"));
        await Assert.ThrowsAsync<ArgumentException>(() => new CustomerService(repository).AddAsync(new CustomerInput(name, email)));
        Assert.Empty(await repository.ListAsync());
    }
    [Fact]
    public async Task NormalizesAndPersistsCustomersAndRejectsDuplicateEmail()
    {
        var path = Path.Combine(directory, "customers.sqlite");
        var service = new CustomerService(new CustomerRepository(path));
        var customer = await service.AddAsync(new(" Marina  Exemplo ", " MARINA@EXAMPLE.COM "));
        Assert.Equal("Marina Exemplo", customer.FullName); Assert.Equal("marina@example.com", customer.Email);
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAsync(new("Outra Pessoa", "marina@example.com")));
        var persisted = Assert.Single(await new CustomerRepository(path).ListAsync()); Assert.Equal(customer, persisted);
    }
    [Fact]
    public async Task DatabaseProtectsAgainstConcurrentDuplicateEmails()
    {
        var service = new CustomerService(new CustomerRepository(Path.Combine(directory, "customers.sqlite")));
        async Task<bool> TryAdd() { try { await service.AddAsync(new("Cliente Exemplo", "same@example.com")); return true; } catch (ArgumentException) { return false; } }
        var results = await Task.WhenAll(Task.Run(TryAdd), Task.Run(TryAdd));
        Assert.Single(results, x => x); Assert.Single(await service.ListAsync());
    }
    [Fact]
    public async Task NormalizesWhitespaceInCustomerName()
    {
        var service = new CustomerService(new CustomerRepository(Path.Combine(directory, "customers.sqlite")));
        var customer = await service.AddAsync(new CustomerInput(" Marina\t\nExemplo ", "marina@example.com"));
        Assert.Equal("Marina Exemplo", customer.FullName);
    }
    [Fact]
    public void RejectsEmptyAttributeName()
    {
        using var repository = new ProductRepository(Path.Combine(directory, "products.db"));
        Assert.Throws<ArgumentException>(() => new ProductService(repository).Add(new Product { Name = "Caderno", Price = 10, Attributes = new() { { " ", "Verde" } } }));
    }
    public void Dispose() => Directory.Delete(directory, true);
}

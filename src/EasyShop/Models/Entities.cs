namespace EasyShop.Models;

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string[] Categories { get; set; } = [];
    public Dictionary<string, string> Attributes { get; set; } = new();
}
public sealed record Customer(int Id, string FullName, string Email);
public sealed record CustomerInput(string FullName, string Email);

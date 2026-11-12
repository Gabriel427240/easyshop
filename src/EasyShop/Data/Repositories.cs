using EasyShop.Models;
using LiteDB;
using Microsoft.Data.Sqlite;

namespace EasyShop.Data;

public interface IProductRepository
{
    Product[] List();
    Product Add(Product product);
}
public sealed class ProductRepository : IProductRepository, IDisposable
{
    private readonly LiteDatabase database;
    public ProductRepository(string path) => database = new LiteDatabase(path);
    public Product[] List() => database.GetCollection<Product>("products").FindAll().OrderByDescending(p => p.Id).ToArray();
    public Product Add(Product product)
    {
        database.GetCollection<Product>("products").Insert(product);
        return product;
    }
    public void Dispose() => database.Dispose();
}
public interface ICustomerRepository
{
    Task<Customer[]> ListAsync();
    Task<Customer> AddAsync(string name, string email);
}
public sealed class CustomerRepository : ICustomerRepository
{
    private readonly string connectionString;
    public CustomerRepository(string path)
    {
        connectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS customers (id INTEGER PRIMARY KEY AUTOINCREMENT, full_name TEXT NOT NULL, email TEXT NOT NULL UNIQUE COLLATE NOCASE)";
        command.ExecuteNonQuery();
    }
    public async Task<Customer[]> ListAsync()
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, full_name, email FROM customers ORDER BY id DESC";
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<Customer>();
        while (await reader.ReadAsync()) results.Add(new Customer(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        return results.ToArray();
    }
    public async Task<Customer> AddAsync(string name, string email)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO customers(full_name,email) VALUES ($name,$email) RETURNING id";
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$email", email);
        try { return new Customer(Convert.ToInt32(await command.ExecuteScalarAsync()), name, email); }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        { throw new ArgumentException("Este e-mail já está cadastrado."); }
    }
}

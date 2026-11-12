using System.Globalization;
using System.Text.RegularExpressions;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using EasyShop.Data;
using EasyShop.Models;
using EasyShop.Services;

namespace EasyShop.AndroidApp;

[Activity(Label = "EasyShop", MainLauncher = true, Exported = true,
    ConfigurationChanges = global::Android.Content.PM.ConfigChanges.Orientation | global::Android.Content.PM.ConfigChanges.ScreenSize)]
public sealed class MainActivity : Activity
{
    private ProductRepository? productRepository;
    private ProductService products = null!;
    private CustomerService customers = null!;
    private LinearLayout content = null!;
    private LinearLayout records = null!;
    private TextView message = null!;
    private TextView count = null!;
    private Button save = null!;
    private EditText name = null!, price = null!, categories = null!, attributes = null!, email = null!;
    private bool productView = true;
    private bool busy;
    private readonly CultureInfo culture = CultureInfo.GetCultureInfo("pt-BR");

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        try
        {
            var directory = FilesDir!.AbsolutePath;
            productRepository = new ProductRepository(Path.Combine(directory, "products.db"));
            products = new ProductService(productRepository);
            customers = new CustomerService(new CustomerRepository(Path.Combine(directory, "customers.sqlite")));
            productView = savedInstanceState?.GetBoolean("products", true) ?? true;
            Render();
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("EasyShop", exception.ToString());
            SetContentView(new TextView(this) { Text = "Não foi possível abrir os dados locais. Feche e abra o aplicativo novamente.", TextSize = 18 });
        }
    }

    private int Dp(int value) => (int)(value * Resources!.DisplayMetrics!.Density);
    private TextView Text(string value, int size = 16)
    {
        var text = new TextView(this) { Text = value, TextSize = size };
        text.SetTextColor(global::Android.Graphics.Color.Rgb(24, 43, 51));
        text.SetPadding(0, Dp(8), 0, Dp(8));
        return text;
    }
    private EditText Field(string label, string id, global::Android.Text.InputTypes type = global::Android.Text.InputTypes.ClassText)
    {
        content.AddView(Text(label, 14));
        var field = new EditText(this) { Hint = label, ContentDescription = id, InputType = type, TextSize = 16 };
        if (id == "price") field.KeyListener = global::Android.Text.Method.DigitsKeyListener.GetInstance("0123456789,.");
        field.SetSingleLine(id != "attributes");
        if (id == "attributes") field.SetMinLines(2);
        content.AddView(field, new LinearLayout.LayoutParams(-1, -2));
        return field;
    }
    private Button Button(string title, string id, LinearLayout parent)
    {
        var button = new Button(this) { Text = title, ContentDescription = id };
        button.SetTextColor(global::Android.Graphics.Color.Rgb(8, 105, 81));
        parent.AddView(button, new LinearLayout.LayoutParams(-1, -2));
        return button;
    }
    private void Render()
    {
        var scroll = new ScrollView(this) { FillViewport = true };
        content = new LinearLayout(this) { Orientation = Orientation.Vertical };
        content.SetPadding(Dp(20), Dp(32), Dp(20), Dp(28));
        content.SetBackgroundColor(global::Android.Graphics.Color.Rgb(245, 247, 249));
        scroll.AddView(content);
        SetContentView(scroll);
        content.AddView(Text("EasyShop", 28));
        content.AddView(Text("Cadastro local • Funciona sem internet", 13));
        var productTab = Button("Produtos", "products-tab", content);
        var customerTab = Button("Clientes", "customers-tab", content);
        productTab.Click += (_, _) => { if (!busy) { productView = true; Render(); } };
        customerTab.Click += (_, _) => { if (!busy) { productView = false; Render(); } };
        content.AddView(Text(productView ? "Catálogo de produtos" : "Cadastro de clientes", 23));
        message = Text("", 15);
        message.ContentDescription = "status";
        message.AccessibilityLiveRegion = global::Android.Views.AccessibilityLiveRegion.Polite;
        content.AddView(message);
        name = Field(productView ? "Nome do produto *" : "Nome completo *", "name");
        if (productView)
        {
            price = Field("Preço R$ * (ex.: 29,90)", "price", global::Android.Text.InputTypes.ClassNumber | global::Android.Text.InputTypes.NumberFlagDecimal);
            categories = Field("Categorias (opcional, separadas por vírgula)", "categories");
            attributes = Field("Atributos (opcional, um nome: valor por linha)", "attributes", global::Android.Text.InputTypes.ClassText | global::Android.Text.InputTypes.TextFlagMultiLine);
        }
        else email = Field("E-mail *", "email", global::Android.Text.InputTypes.ClassText | global::Android.Text.InputTypes.TextVariationEmailAddress);
        save = Button(productView ? "Cadastrar produto" : "Cadastrar cliente", "save", content);
        save.Click += async (_, _) => await SaveAsync();
        var refresh = Button("Atualizar lista", "refresh", content);
        refresh.Click += async (_, _) => { if (!busy) await LoadAsync(); };
        count = Text("Carregando…", 18);
        content.AddView(count);
        records = new LinearLayout(this) { Orientation = Orientation.Vertical, ContentDescription = "records" };
        content.AddView(records);
        _ = LoadAsync();
    }
    private void Status(string text, bool error = false)
    {
        message.Text = text;
        message.SetTextColor(error ? global::Android.Graphics.Color.Rgb(160, 45, 40) : global::Android.Graphics.Color.Rgb(8, 105, 81));
    }
    private async Task LoadAsync()
    {
        busy = true;
        save.Enabled = false;
        try
        {
            records.RemoveAllViews();
            if (productView)
            {
                var items = await Task.Run(products.List);
                count.Text = $"Produtos cadastrados: {items.Length}";
                if (items.Length == 0) records.AddView(Text("Seu catálogo começa aqui. Cadastre o primeiro produto."));
                foreach (var item in items)
                {
                    records.AddView(Text($"{item.Name} • {item.Price.ToString("C", culture)}", 18));
                    if (item.Categories.Length > 0) records.AddView(Text(string.Join(" · ", item.Categories), 14));
                    if (item.Attributes.Count > 0) records.AddView(Text(string.Join(" · ", item.Attributes.Select(a => $"{a.Key}: {a.Value}")), 14));
                }
            }
            else
            {
                var items = await Task.Run(customers.ListAsync);
                count.Text = $"Clientes cadastrados: {items.Length}";
                if (items.Length == 0) records.AddView(Text("Seus contatos começam aqui. Cadastre o primeiro cliente."));
                foreach (var item in items) { records.AddView(Text(item.FullName, 18)); records.AddView(Text(item.Email, 14)); }
            }
        }
        catch (Exception exception) { Report(exception); }
        finally { busy = false; save.Enabled = true; }
    }
    private async Task SaveAsync()
    {
        if (busy) return;
        busy = true;
        save.Enabled = false;
        Status("Salvando…");
        try
        {
            if (productView)
            {
                var raw = price.Text?.Trim() ?? "";
                if (!Regex.IsMatch(raw, @"^\d+(?:[,.]\d{1,2})?$") || !decimal.TryParse(raw.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    throw new ArgumentException("Use um preço como 29,90, sem separador de milhar.");
                var values = new Dictionary<string, string>();
                foreach (var line in (attributes.Text ?? "").Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    var separator = line.IndexOf(':');
                    if (separator < 1 || string.IsNullOrWhiteSpace(line[(separator + 1)..])) throw new ArgumentException("Use atributos no formato nome: valor.");
                    if (!values.TryAdd(line[..separator].Trim(), line[(separator + 1)..].Trim())) throw new ArgumentException("Não repita o nome de um atributo.");
                }
                var input = new Product { Name = name.Text ?? "", Price = amount, Categories = (categories.Text ?? "").Split(',').Select(c => c.Trim()).Where(c => c.Length > 0).ToArray(), Attributes = values };
                await Task.Run(() => products.Add(input));
                price.Text = categories.Text = attributes.Text = "";
            }
            else
            {
                var input = new CustomerInput(name.Text ?? "", email.Text ?? "");
                await Task.Run(() => customers.AddAsync(input));
                email.Text = "";
            }
            name.Text = "";
            if (GetSystemService(InputMethodService) is InputMethodManager keyboard) keyboard.HideSoftInputFromWindow(name.WindowToken, HideSoftInputFlags.None);
            Status("Cadastro realizado com sucesso.");
            await LoadAsync();
        }
        catch (Exception exception) { Report(exception); }
        finally { busy = false; save.Enabled = true; }
    }
    private void Report(Exception exception)
    {
        global::Android.Util.Log.Error("EasyShop", exception.ToString());
        Status(exception is ArgumentException ? exception.Message : "Não foi possível acessar os dados. Tente atualizar a lista.", true);
    }
    protected override void OnSaveInstanceState(Bundle outState)
    {
        outState.PutBoolean("products", productView);
        base.OnSaveInstanceState(outState);
    }
    protected override void OnDestroy()
    {
        productRepository?.Dispose();
        base.OnDestroy();
    }
}

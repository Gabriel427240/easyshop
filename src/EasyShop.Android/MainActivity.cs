using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Android.Graphics;
using Android.Graphics.Drawables;
using Path = System.IO.Path;
using EasyShop.Data;
using EasyShop.Models;
using EasyShop.Services;

namespace EasyShop.AndroidApp;

[Activity(Label = "EasyShop", MainLauncher = true, Exported = true,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = global::Android.Content.PM.ConfigChanges.Orientation | global::Android.Content.PM.ConfigChanges.ScreenSize)]
public sealed class MainActivity : Activity
{
    private ProductRepository? productRepository;
    private ProductService products = null!;
    private CustomerService customers = null!;
    private LinearLayout content = null!, form = null!, records = null!;
    private TextView message = null!, count = null!;
    private Button save = null!, refresh = null!, productTab = null!, customerTab = null!;
    private EditText name = null!, price = null!, categories = null!, attributes = null!, email = null!;
    private readonly List<EditText> fields = [];
    private readonly Dictionary<EditText, TextView> fieldErrors = [];
    private readonly Dictionary<string, string> drafts = [];
    private bool productView = true, busy;
    private readonly CultureInfo culture = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly Color Ink = Color.Rgb(23, 47, 43), Muted = Color.Rgb(87, 109, 103), Green = Color.Rgb(17, 107, 83);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window!.SetSoftInputMode(SoftInput.AdjustResize);
        try
        {
            var directory = FilesDir!.AbsolutePath;
            productRepository = new ProductRepository(Path.Combine(directory, "products.db"));
            products = new ProductService(productRepository);
            customers = new CustomerService(new CustomerRepository(Path.Combine(directory, "customers.sqlite")));
            productView = savedInstanceState?.GetBoolean("products", true) ?? true;
            foreach (var key in new[] { "p-name", "p-price", "p-categories", "p-attributes", "c-name", "c-email" })
                drafts[key] = savedInstanceState?.GetString(key) ?? "";
            Render();
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("EasyShop", exception.ToString());
            SetContentView(new TextView(this) { Text = "Não foi possível abrir os dados locais. Feche e abra o aplicativo novamente.", TextSize = 18 });
        }
    }

    private int Dp(int value) => (int)(value * Resources!.DisplayMetrics!.Density);
    private GradientDrawable Surface(Color color, int radius = 16, bool border = false)
    {
        var shape = new GradientDrawable();
        shape.SetColor(color);
        shape.SetCornerRadius(Dp(radius));
        if (border) shape.SetStroke(Dp(1), Color.Rgb(215, 226, 220));
        return shape;
    }
    private TextView Text(string value, int size = 16, bool bold = false)
    {
        var text = new TextView(this) { Text = value, TextSize = size };
        text.SetTextColor(Ink);
        if (bold) text.SetTypeface(Typeface.Default, TypefaceStyle.Bold);
        text.SetPadding(0, Dp(5), 0, Dp(5));
        return text;
    }
    private void Hint(LinearLayout parent, string value)
    {
        var text = Text(value, 12);
        text.SetTextColor(Muted);
        parent.AddView(text);
    }
    private LinearLayout Card(LinearLayout parent)
    {
        var card = new LinearLayout(this) { Orientation = Orientation.Vertical };
        card.SetPadding(Dp(18), Dp(16), Dp(18), Dp(18));
        card.Background = Surface(Color.White, 20, true);
        parent.AddView(card, new LinearLayout.LayoutParams(-1, -2) { TopMargin = Dp(16) });
        return card;
    }
    private EditText Field(string label, string placeholder, string id, int max,
        global::Android.Text.InputTypes type = global::Android.Text.InputTypes.ClassText)
    {
        var caption = Text(label, 14, true);
        form.AddView(caption, new LinearLayout.LayoutParams(-1, -2) { TopMargin = Dp(12) });
        var field = new EditText(this)
        {
            Id = View.GenerateViewId(), Hint = placeholder, ContentDescription = id,
            InputType = type, TextSize = 16, Focusable = true, FocusableInTouchMode = true,
            ShowSoftInputOnFocus = true, ImeOptions = ImeAction.Next
        };
        caption.LabelFor = field.Id;
        field.SetTextColor(Ink);
        field.SetHintTextColor(Muted);
        field.SetPadding(Dp(12), Dp(12), Dp(12), Dp(12));
        field.SetMinHeight(Dp(54));
        field.Background = Surface(Color.Rgb(248, 250, 248), 10, true);
        field.SetFilters([new global::Android.Text.InputFilterLengthFilter(max)]);
        if (id == "price") field.KeyListener = global::Android.Text.Method.DigitsKeyListener.GetInstance("0123456789,.");
        field.SetSingleLine(id != "attributes");
        if (id == "attributes") { field.SetMinLines(2); field.Gravity = GravityFlags.Top; field.ImeOptions = ImeAction.None; }
        field.Text = drafts.GetValueOrDefault((productView ? "p-" : "c-") + id, "");
        field.FocusChange += (_, e) =>
        {
            if (e.HasFocus) field.PostDelayed(() => ShowKeyboard(field), 150);
        };
        field.Click += (_, _) => ShowKeyboard(field);
        form.AddView(field, new LinearLayout.LayoutParams(-1, -2));
        var error = Text("", 12);
        error.SetTextColor(Color.Rgb(170, 40, 40));
        error.Visibility = ViewStates.Gone;
        error.AccessibilityLiveRegion = AccessibilityLiveRegion.Polite;
        form.AddView(error);
        fieldErrors[field] = error;
        field.TextChanged += (_, _) =>
        {
            error.Visibility = ViewStates.Gone;
            field.Background = Surface(Color.Rgb(248, 250, 248), 10, true);
        };
        fields.Add(field);
        return field;
    }
    private void ShowKeyboard(EditText field)
    {
        if (!field.HasFocus) return;
        if (GetSystemService(InputMethodService) is InputMethodManager keyboard)
            keyboard.ShowSoftInput(field, ShowFlags.Implicit);
    }
    private void HideKeyboard()
    {
        if (GetSystemService(InputMethodService) is InputMethodManager keyboard)
            keyboard.HideSoftInputFromWindow(name.WindowToken, HideSoftInputFlags.None);
        content.RequestFocus();
    }
    private Button ActionButton(string title, string id, LinearLayout parent, bool primary = false)
    {
        var button = new Button(this) { Text = title, ContentDescription = id, TextSize = 14 };
        button.SetAllCaps(false);
        button.SetMinHeight(Dp(50));
        button.SetPadding(Dp(12), Dp(8), Dp(12), Dp(8));
        button.SetTextColor(primary ? Color.White : Green);
        button.Background = Surface(primary ? Green : Color.Rgb(233, 243, 236), 12);
        parent.AddView(button, new LinearLayout.LayoutParams(-1, -2) { TopMargin = Dp(12) });
        return button;
    }
    private void CaptureDraft()
    {
        foreach (var field in fields) drafts[(productView ? "p-" : "c-") + field.ContentDescription] = field.Text ?? "";
    }
    private void SwitchTab(bool showProducts)
    {
        if (busy || productView == showProducts) return;
        CaptureDraft();
        HideKeyboard();
        productView = showProducts;
        Render();
    }
    private void Render()
    {
        fields.Clear();
        fieldErrors.Clear();
        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.SetBackgroundColor(Color.Rgb(244, 247, 243));
        var scroll = new ScrollView(this) { FillViewport = true };
        content = new LinearLayout(this) { Orientation = Orientation.Vertical, Focusable = true, FocusableInTouchMode = true };
        content.SetPadding(Dp(20), Dp(16), Dp(20), Dp(24));
        scroll.AddView(content);
        root.AddView(scroll, new LinearLayout.LayoutParams(-1, -1));
        SetContentView(root);
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var appearance = (int)(WindowInsetsControllerAppearance.LightStatusBars | WindowInsetsControllerAppearance.LightNavigationBars);
            Window!.InsetsController?.SetSystemBarsAppearance(appearance, appearance);
            root.SetOnApplyWindowInsetsListener(new InsetsListener(root));
            root.RequestApplyInsets();
        }
        content.AddView(Text("EasyShop", 30, true));
        Hint(content, "Seu negócio, organizado. Tudo salvo neste dispositivo.");
        var tabs = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        content.AddView(tabs, new LinearLayout.LayoutParams(-1, -2) { TopMargin = Dp(12) });
        productTab = ActionButton("Produtos", "products-tab", tabs, productView);
        customerTab = ActionButton("Clientes", "customers-tab", tabs, !productView);
        productTab.LayoutParameters = new LinearLayout.LayoutParams(0, Dp(50), 1) { RightMargin = Dp(4) };
        customerTab.LayoutParameters = new LinearLayout.LayoutParams(0, Dp(50), 1) { LeftMargin = Dp(4) };
        productTab.Selected = productView;
        customerTab.Selected = !productView;
        productTab.Click += (_, _) => SwitchTab(true);
        customerTab.Click += (_, _) => SwitchTab(false);
        form = Card(content);
        form.AddView(Text(productView ? "Novo produto" : "Novo cliente", 23, true));
        Hint(form, productView ? "Preencha nome e preço para começar. * Obrigatório" : "Organize seus contatos. * Obrigatório");
        name = Field(productView ? "Nome do produto *" : "Nome completo *", productView ? "Ex.: Caderno pontilhado" : "Ex.: Marina Silva", "name", productView ? 120 : 160,
            global::Android.Text.InputTypes.ClassText | (productView ? global::Android.Text.InputTypes.TextFlagCapSentences : global::Android.Text.InputTypes.TextFlagCapWords));
        if (productView)
        {
            Hint(form, "De 2 a 120 caracteres.");
            price = Field("Preço (R$) *", "Ex.: 29,90", "price", 12, global::Android.Text.InputTypes.ClassNumber | global::Android.Text.InputTypes.NumberFlagDecimal);
            Hint(form, "Maior que zero. Até 2 casas decimais, sem separador de milhar.");
            var optional = Text("DETALHES OPCIONAIS", 12, true);
            form.AddView(optional, new LinearLayout.LayoutParams(-1, -2) { TopMargin = Dp(16) });
            categories = Field("Categorias", "Ex.: Papelaria, Escritório", "categories", 1239);
            Hint(form, "Separe por vírgula. Até 20 categorias de 60 caracteres.");
            attributes = Field("Atributos", "Cor: Verde\nTamanho: A5", "attributes", 5259,
                global::Android.Text.InputTypes.ClassText | global::Android.Text.InputTypes.TextFlagMultiLine | global::Android.Text.InputTypes.TextFlagCapSentences);
            Hint(form, "Um nome: valor por linha. Até 20; nome até 60 e valor até 200 caracteres.");
        }
        else
        {
            Hint(form, "Informe nome e sobrenome, até 160 caracteres.");
            email = Field("E-mail *", "Ex.: marina@exemplo.com", "email", 254,
                global::Android.Text.InputTypes.ClassText | global::Android.Text.InputTypes.TextVariationEmailAddress);
            Hint(form, "Use um e-mail válido. Cada e-mail pode ser cadastrado uma única vez.");
            email.ImeOptions = ImeAction.Done;
        }
        for (int i = 0; i < fields.Count - 1; i++)
        {
            var next = fields[i + 1];
            fields[i].NextFocusForwardId = next.Id;
            fields[i].EditorAction += (_, e) => { if (e.ActionId == ImeAction.Next) { next.RequestFocus(); e.Handled = true; } };
        }
        if (!productView) email.EditorAction += (_, e) => { if (e.ActionId == ImeAction.Done) { HideKeyboard(); e.Handled = true; } };
        save = ActionButton(productView ? "Cadastrar produto" : "Cadastrar cliente", "save", form, true);
        save.Click += async (_, _) => await SaveAsync();
        message = Text("", 14);
        message.ContentDescription = "status";
        message.Visibility = ViewStates.Gone;
        message.AccessibilityLiveRegion = AccessibilityLiveRegion.Polite;
        form.AddView(message);
        var list = Card(content);
        list.AddView(Text(productView ? "Seu catálogo" : "Seus clientes", 22, true));
        count = Text("Carregando…", 14);
        list.AddView(count);
        refresh = ActionButton("Atualizar lista", "refresh", list);
        refresh.Click += async (_, _) => { if (!busy) await LoadAsync(); };
        records = new LinearLayout(this) { Orientation = Orientation.Vertical, ContentDescription = "records" };
        list.AddView(records);
        Hint(content, "Funciona sem internet • Os dados permanecem ao fechar o app.");
        content.RequestFocus();
        _ = LoadAsync();
    }
    [System.Runtime.Versioning.SupportedOSPlatform("android30.0")]
    private sealed class InsetsListener(View root) : Java.Lang.Object, View.IOnApplyWindowInsetsListener
    {
        public WindowInsets OnApplyWindowInsets(View? view, WindowInsets? insets)
        {
            if (insets == null) return insets!;
            var bars = insets.GetInsets(WindowInsets.Type.SystemBars());
            var ime = insets.GetInsets(WindowInsets.Type.Ime());
            root.SetPadding(bars.Left, bars.Top, bars.Right, Math.Max(bars.Bottom, ime.Bottom));
            return insets;
        }
    }
    private void SetBusy(bool value)
    {
        busy = value;
        save.Enabled = refresh.Enabled = productTab.Enabled = customerTab.Enabled = !value;
        foreach (var field in fields) field.Enabled = !value;
        save.Text = value ? "Aguarde…" : productView ? "Cadastrar produto" : "Cadastrar cliente";
        save.Alpha = value ? .55f : 1f;
    }
    private void Status(string text, bool error = false)
    {
        message.Text = text;
        message.Visibility = ViewStates.Visible;
        message.SetTextColor(error ? Color.Rgb(170, 40, 40) : Green);
    }
    private async Task LoadAsync()
    {
        SetBusy(true);
        try
        {
            if (productView)
            {
                var items = await Task.Run(products.List);
                records.RemoveAllViews();
                count.Text = $"{items.Length} produto(s) cadastrado(s)";
                if (items.Length == 0) Hint(records, "Seu catálogo começa aqui. Cadastre seu primeiro produto acima.");
                foreach (var item in items)
                {
                    var card = Card(records);
                    card.AddView(Text(item.Name, 18, true));
                    var amount = Text(item.Price.ToString("C", culture), 21, true);
                    amount.SetTextColor(Green);
                    card.AddView(amount);
                    if (item.Categories.Length > 0) Hint(card, string.Join(" · ", item.Categories));
                    if (item.Attributes.Count > 0) Hint(card, string.Join("\n", item.Attributes.Select(a => $"{a.Key}: {a.Value}")));
                }
            }
            else
            {
                var items = await Task.Run(customers.ListAsync);
                records.RemoveAllViews();
                count.Text = $"{items.Length} cliente(s) cadastrado(s)";
                if (items.Length == 0) Hint(records, "Sua lista começa aqui. Cadastre seu primeiro cliente acima.");
                foreach (var item in items) { var card = Card(records); card.AddView(Text(item.FullName, 18, true)); Hint(card, item.Email); }
            }
        }
        catch (Exception exception) { Report(exception); count.Text = "Não foi possível atualizar. Toque em Atualizar lista para tentar novamente."; }
        finally { SetBusy(false); }
    }
    private bool Invalid(EditText field, string error)
    {
        fieldErrors[field].Text = error;
        fieldErrors[field].Visibility = ViewStates.Visible;
        field.RequestFocus();
        field.PostDelayed(() => field.RequestRectangleOnScreen(new Rect(0, 0, field.Width, field.Height + Dp(60)), false), 250);
        Status("Revise o campo indicado para continuar.", true);
        return false;
    }
    private bool Validate(out decimal amount, out Dictionary<string, string> values)
    {
        amount = 0;
        values = [];
        var normalizedName = string.Join(' ', (name.Text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (productView)
        {
            if (normalizedName.Length is < 2 or > 120) return Invalid(name, "Use entre 2 e 120 caracteres.");
            var raw = price.Text?.Trim() ?? "";
            if (!Regex.IsMatch(raw, @"^\d+(?:[,.]\d{1,2})?$") || !decimal.TryParse(raw.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) || amount <= 0 || amount > 999999999.99m)
                return Invalid(price, "Informe de R$ 0,01 a R$ 999.999.999,99. Ex.: 29,90, sem separador de milhar.");
            var tags = (categories.Text ?? "").Split(',').Select(c => c.Trim()).Where(c => c.Length > 0).ToArray();
            if (tags.Length > 20 || tags.Any(c => c.Length > 60)) return Invalid(categories, "Use até 20 categorias com até 60 caracteres cada.");
            foreach (var line in (attributes.Text ?? "").Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var separator = line.IndexOf(':');
                if (separator < 1 || string.IsNullOrWhiteSpace(line[(separator + 1)..])) return Invalid(attributes, "Use um atributo por linha, como Cor: Verde.");
                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim();
                if (key.Length is < 1 or > 60 || value.Length > 200) return Invalid(attributes, "Nome até 60 caracteres e valor até 200 caracteres.");
                if (!values.TryAdd(key, value)) return Invalid(attributes, "Não repita o nome de um atributo.");
                if (values.Count > 20) return Invalid(attributes, "Use até 20 atributos.");
            }
        }
        else
        {
            if (normalizedName.Length is < 3 or > 160 || normalizedName.Split(' ').Length < 2) return Invalid(name, "Informe nome e sobrenome, até 160 caracteres.");
            var address = (email.Text ?? "").Trim().ToLowerInvariant();
            if (address.Length > 254 || !MailAddress.TryCreate(address, out var parsed) || parsed.Address != address || !parsed.Host.Contains('.'))
                return Invalid(email, "Informe um e-mail válido, como nome@exemplo.com.");
        }
        return true;
    }
    private async Task SaveAsync()
    {
        if (busy || !Validate(out var amount, out var values)) return;
        HideKeyboard();
        SetBusy(true);
        Status("Salvando…");
        try
        {
            if (productView)
            {
                var input = new Product { Name = name.Text ?? "", Price = amount, Categories = (categories.Text ?? "").Split(',').Select(c => c.Trim()).Where(c => c.Length > 0).ToArray(), Attributes = values };
                await Task.Run(() => products.Add(input));
            }
            else
            {
                var input = new CustomerInput(name.Text ?? "", email.Text ?? "");
                await Task.Run(() => customers.AddAsync(input));
            }
            foreach (var field in fields) { field.Text = ""; field.Error = null; }
            CaptureDraft();
            Status(productView ? "Produto cadastrado! Você já pode adicionar outro." : "Cliente cadastrado! Você já pode adicionar outro.");
            await LoadAsync();
        }
        catch (Exception exception) { Report(exception); }
        finally { SetBusy(false); }
    }
    private void Report(Exception exception)
    {
        global::Android.Util.Log.Error("EasyShop", exception.ToString());
        Status(exception is ArgumentException ? exception.Message : "Não foi possível concluir. Seus campos foram mantidos; tente novamente.", true);
    }
    protected override void OnSaveInstanceState(Bundle outState)
    {
        CaptureDraft();
        outState.PutBoolean("products", productView);
        foreach (var draft in drafts) outState.PutString(draft.Key, draft.Value);
        base.OnSaveInstanceState(outState);
    }
    protected override void OnDestroy()
    {
        productRepository?.Dispose();
        base.OnDestroy();
    }
}

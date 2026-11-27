using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Shape = System.Windows.Shapes.Path;

internal sealed class NetLocation
{
    private static readonly HttpClient Geo = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly HttpClient Speed = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly string Settings = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetLocation", "expected.txt");
    private static readonly string ScaleSettings = Path.Combine(Path.GetDirectoryName(Settings), "scale.txt");

    private static readonly Brush Ink = ColorBrush("#202126");
    private static readonly Brush Muted = ColorBrush("#626570");
    private static readonly Brush Accent = ColorBrush("#163C72");
    private readonly TextBlock location = Label("Определяем локацию…", 20, Ink);
    private readonly TextBlock locationStatus = Label("", 12, Muted);
    private readonly TextBlock down = Label("—", 21, Accent);
    private readonly TextBlock up = Label("—", 21, Accent);
    private readonly TextBlock speedStatus = Label("Скорость ещё не измерена", 12, Muted);
    private readonly Shape flame = new Shape {
        Width = 21, Height = 25, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 7, 0),
        Fill = new LinearGradientBrush(Color.FromRgb(255, 213, 111), Color.FromRgb(255, 83, 36), 90),
        Data = Geometry.Parse("M 12,0 C 16,8 7,9 11,15 C 13,13 17,10 17,6 C 22,12 24,19 19,24 C 15,29 7,28 3,24 C -2,19 0,12 6,8 C 5,13 6,14 7,14 C 5,8 10,5 12,0 Z"),
        Visibility = Visibility.Collapsed
    };
    private Task locationRequest;
    private static readonly string[] Countries = { "Австралия, AU", "Австрия, AT", "Азербайджан, AZ", "Аландские о-ва, AX", "Албания, AL", "Алжир, DZ", "Американское Самоа, AS", "Ангилья, AI", "Ангола, AO", "Андорра, AD", "Антарктида, AQ", "Антигуа и Барбуда, AG", "Аргентина, AR", "Армения, AM", "Аруба, AW", "Афганистан, AF", "Багамы, BS", "Бангладеш, BD", "Барбадос, BB", "Бахрейн, BH", "Беларусь, BY", "Белиз, BZ", "Бельгия, BE", "Бенин, BJ", "Бермудские о-ва, BM", "Болгария, BG", "Боливия, BO", "Бонэйр, Синт-Эстатиус и Саба, BQ", "Босния и Герцеговина, BA", "Ботсвана, BW", "Бразилия, BR", "Британская территория в Индийском океане, IO", "Бруней, BN", "Буркина-Фасо, BF", "Бурунди, BI", "Бутан, BT", "Вануату, VU", "Ватикан, VA", "Великобритания, GB", "Венгрия, HU", "Венесуэла, VE", "Виргинские о-ва (Великобритания), VG", "Виргинские о-ва (США), VI", "Внешние малые о-ва (США), UM", "Восточный Тимор, TL", "Вьетнам, VN", "Габон, GA", "Гаити, HT", "Гайана, GY", "Гамбия, GM", "Гана, GH", "Гваделупа, GP", "Гватемала, GT", "Гвинея-Бисау, GW", "Гвинея, GN", "Германия, DE", "Гернси, GG", "Гибралтар, GI", "Гондурас, HN", "Гонконг (САР), HK", "Гренада, GD", "Гренландия, GL", "Греция, GR", "Грузия, GE", "Гуам, GU", "Дания, DK", "Джерси, JE", "Джибути, DJ", "Доминика, DM", "Доминиканская Республика, DO", "Египет, EG", "Замбия, ZM", "Западная Сахара, EH", "Зимбабве, ZW", "Израиль, IL", "Индия, IN", "Индонезия, ID", "Иордания, JO", "Ирак, IQ", "Иран, IR", "Ирландия, IE", "Исландия, IS", "Испания, ES", "Италия, IT", "Йемен, YE", "Кабо-Верде, CV", "Казахстан, KZ", "Камбоджа, KH", "Камерун, CM", "Канада, CA", "Катар, QA", "Кения, KE", "Кипр, CY", "Киргизия, KG", "Кирибати, KI", "Китай, CN", "КНДР, KP", "Кокосовые о-ва, CC", "Колумбия, CO", "Коморы, KM", "Конго - Браззавиль, CG", "Конго - Киншаса, CD", "Косово, XK", "Коста-Рика, CR", "Кот-д’Ивуар, CI", "Куба, CU", "Кувейт, KW", "Кюрасао, CW", "Лаос, LA", "Латвия, LV", "Лесото, LS", "Либерия, LR", "Ливан, LB", "Ливия, LY", "Литва, LT", "Лихтенштейн, LI", "Люксембург, LU", "Маврикий, MU", "Мавритания, MR", "Мадагаскар, MG", "Майотта, YT", "Макао (САР), MO", "Малави, MW", "Малайзия, MY", "Мали, ML", "Мальдивы, MV", "Мальта, MT", "Марокко, MA", "Мартиника, MQ", "Маршалловы о-ва, MH", "Мексика, MX", "Мозамбик, MZ", "Молдова, MD", "Монако, MC", "Монголия, MN", "Монтсеррат, MS", "Мьянма (Бирма), MM", "Намибия, NA", "Науру, NR", "Непал, NP", "Нигер, NE", "Нигерия, NG", "Нидерланды, NL", "Никарагуа, NI", "Ниуэ, NU", "Новая Зеландия, NZ", "Новая Каледония, NC", "Норвегия, NO", "о-в Буве, BV", "о-в Мэн, IM", "о-в Норфолк, NF", "о-в Рождества, CX", "о-в Св. Елены, SH", "о-ва Кайман, KY", "о-ва Кука, CK", "о-ва Питкэрн, PN", "о-ва Херд и Макдональд, HM", "ОАЭ, AE", "Оман, OM", "Пакистан, PK", "Палау, PW", "Палестинские территории, PS", "Панама, PA", "Папуа — Новая Гвинея, PG", "Парагвай, PY", "Перу, PE", "Польша, PL", "Португалия, PT", "Пуэрто-Рико, PR", "Республика Корея, KR", "Реюньон, RE", "Россия, RU", "Руанда, RW", "Румыния, RO", "Сальвадор, SV", "Самоа, WS", "Сан-Марино, SM", "Сан-Томе и Принсипи, ST", "Саудовская Аравия, SA", "Северная Македония, MK", "Северные Марианские о-ва, MP", "Сейшельские о-ва, SC", "Сен-Бартелеми, BL", "Сен-Мартен, MF", "Сен-Пьер и Микелон, PM", "Сенегал, SN", "Сент-Винсент и Гренадины, VC", "Сент-Китс и Невис, KN", "Сент-Люсия, LC", "Сербия, RS", "Сингапур, SG", "Синт-Мартен, SX", "Сирия, SY", "Словакия, SK", "Словения, SI", "Соединенные Штаты, US", "Соломоновы о-ва, SB", "Сомали, SO", "Судан, SD", "Суринам, SR", "Сьерра-Леоне, SL", "Таджикистан, TJ", "Таиланд, TH", "Тайвань, TW", "Танзания, TZ", "Тёркс и Кайкос, TC", "Того, TG", "Токелау, TK", "Тонга, TO", "Тринидад и Тобаго, TT", "Тувалу, TV", "Тунис, TN", "Туркменистан, TM", "Турция, TR", "Уганда, UG", "Узбекистан, UZ", "Украина, UA", "Уоллис и Футуна, WF", "Уругвай, UY", "Фарерские о-ва, FO", "Федеративные Штаты Микронезии, FM", "Фиджи, FJ", "Филиппины, PH", "Финляндия, FI", "Фолклендские о-ва, FK", "Франция, FR", "Французская Гвиана, GF", "Французская Полинезия, PF", "Французские Южные территории, TF", "Хорватия, HR", "Центрально-Африканская Республика, CF", "Чад, TD", "Черногория, ME", "Чехия, CZ", "Чили, CL", "Швейцария, CH", "Швеция, SE", "Шпицберген и Ян-Майен, SJ", "Шри-Ланка, LK", "Эквадор, EC", "Экваториальная Гвинея, GQ", "Эритрея, ER", "Эсватини, SZ", "Эстония, EE", "Эфиопия, ET", "Южная Георгия и Южные Сандвичевы о-ва, GS", "Южно-Африканская Республика, ZA", "Южный Судан, SS", "Ямайка, JM", "Япония, JP" };
    private readonly Button measure;
    private readonly Grid fire = new Grid { IsHitTestVisible = false, Visibility = Visibility.Collapsed, ClipToBounds = true };
    private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(2) };
    private string expected;
    private readonly System.Windows.Forms.Form host = new System.Windows.Forms.Form();
    private const string Title = "Location control. LLC Xoma Gold";
    private static readonly FontFamily FontFamily = new FontFamily("Segoe UI");
    private double windowAlpha = 1;
    private GeoResult detected;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--self-test"))
        {
            var widget = new NetLocation();
            widget.detected = new GeoResult { city = "Амстердам", country = "Нидерланды", country_code = "NL" };
            widget.expected = " амстердам , nl ";
            widget.RenderLocation();
            if (!widget.host.TopMost || widget.flame.Visibility != Visibility.Collapsed) return 1;
            foreach (string value in new[] { "Нидерланды", "NL", "Роттердам, Нидерланды", "Нидерланды, Роттердам", "NL, Амстердам", ", NL" })
            {
                widget.expected = value;
                widget.RenderLocation();
                if (widget.flame.Visibility != Visibility.Collapsed) return 4;
            }
            widget.expected = "Амстердам, BE";
            widget.RenderLocation();
            if (widget.flame.Visibility != Visibility.Visible || widget.fire.Visibility != Visibility.Visible || !widget.fire.HasAnimatedProperties) return 5;
            widget.expected = "NL";
            widget.RenderLocation();
            if (widget.fire.Visibility != Visibility.Collapsed || widget.fire.HasAnimatedProperties) return 8;
            widget.expected = "Роттердам";
            widget.RenderLocation();
            if (widget.flame.Visibility != Visibility.Visible) return 2;
            widget.detected = null;
            widget.RenderLocation();
            if (widget.flame.Visibility != Visibility.Collapsed || widget.fire.Visibility != Visibility.Collapsed) return 3;
            IntPtr hwnd = widget.host.Handle;
            if ((GetStyle(hwnd, -16) & 0x40000) == 0) return 6;
            foreach (double value in new[] { 0.25, 0.60, 0.85 }) {
                widget.ApplyOpacity(value);
                uint key, flags; byte alpha;
                if (!GetLayeredWindowAttributes(hwnd, out key, out alpha, out flags) || alpha != (byte)(255 * value) || (flags & 2) == 0) return 7;
            }
            return 0;
        }
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.Run(new NetLocation().host);
        return 0;
    }

    private NetLocation()
    {
        host.Text = Title;
        host.ShowIcon = false;
        host.TopMost = true;
        host.MinimumSize = new System.Drawing.Size(312,218);
        host.Size = new System.Drawing.Size(378,228);
        expected = ReadSetting(Settings);

        var root = new StackPanel { Margin = new Thickness(14, 10, 14, 10) };
        var layers = new Grid();
        var wash = new LinearGradientBrush();
        wash.StartPoint = new Point(0,0);
        wash.EndPoint = new Point(0.1,1);
        wash.GradientStops.Add(new GradientStop(Color.FromRgb(255,160,69),0));
        wash.GradientStops.Add(new GradientStop(Color.FromRgb(255,228,183),0.45));
        wash.GradientStops.Add(new GradientStop(Color.FromRgb(255,126,45),1));
        fire.Background = wash;
        var tongues = new Shape { Data = Geometry.Parse("M0,40 L0,12 Q5,20 8,23 Q17,18 14,4 Q30,15 27,28 Q45,25 43,11 Q53,21 56,25 Q68,16 63,0 Q85,15 78,28 Q90,27 94,12 Q98,18 100,24 L100,40 Z"), Stretch = Stretch.Fill, Height = 22, VerticalAlignment = VerticalAlignment.Bottom, Fill = new LinearGradientBrush(Colors.Gold,Colors.OrangeRed,90) };
        fire.Children.Add(tongues);
        layers.Children.Add(fire);
        layers.Children.Add(root);
        var card = new Border {
            Background = ColorBrush("#F6F6F8"), BorderBrush = ColorBrush("#F6F6F8"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(13), Child = layers
        };
        host.Controls.Add(new System.Windows.Forms.Integration.ElementHost { Dock = System.Windows.Forms.DockStyle.Fill, Child = card });
        double dim;
        if (Double.TryParse(ReadSetting(ScaleSettings + ".opacity"), NumberStyles.Float, CultureInfo.InvariantCulture, out dim) && dim >= 0.25 && dim <= 1) windowAlpha = dim;
        ApplyOpacity(windowAlpha);
        var dimensions = ReadSetting(ScaleSettings + ".size").Split(' ');
        int w, h;
        if (dimensions.Length == 2 && Int32.TryParse(dimensions[0], out w) && Int32.TryParse(dimensions[1], out h)) { host.Size = new System.Drawing.Size(Math.Min(w,System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width),Math.Min(h,System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height)); }
        var place = new DockPanel();
        var settings = ActionButton("⚙", "Локация и прозрачность");
        settings.Click += (s,e) => EditExpected();
        DockPanel.SetDock(settings, Dock.Right);
        place.Children.Add(settings);
        place.Children.Add(flame);
        location.FontWeight = FontWeights.SemiBold;
        location.TextTrimming = TextTrimming.CharacterEllipsis;
        place.Children.Add(location);
        root.Children.Add(place);
        locationStatus.TextTrimming = TextTrimming.CharacterEllipsis;
        root.Children.Add(locationStatus);

        var metrics = new Grid { Margin = new Thickness(0, 9, 0, 8) };
        metrics.ColumnDefinitions.Add(new ColumnDefinition());
        metrics.ColumnDefinitions.Add(new ColumnDefinition());
        AddMetric(metrics, 0, "↓ Скачивание", down);
        AddMetric(metrics, 1, "↑ Отдача", up);
        root.Children.Add(metrics);

        var footer = new DockPanel();
        measure = ActionButton("Проверить", "Проверить скорость и локацию");
        measure.Click += async (s, e) => await CheckAll();
        DockPanel.SetDock(measure, Dock.Right);
        footer.Children.Add(measure);
        speedStatus.VerticalAlignment = VerticalAlignment.Center;
        speedStatus.TextTrimming = TextTrimming.CharacterEllipsis;
        footer.Children.Add(speedStatus);
        root.Children.Add(footer);

        timer.Tick += async (s, e) => await RefreshLocation();
        host.Shown += async (s, e) => {
            var area = System.Windows.Forms.Screen.FromControl(host).WorkingArea;
            host.Location = new System.Drawing.Point(area.Right - host.Width - 16,area.Top + 16);
            timer.Start();
            await CheckAll();
        };
        host.FormClosed += (s, e) => { timer.Stop(); try { Directory.CreateDirectory(Path.GetDirectoryName(Settings)); File.WriteAllText(ScaleSettings + ".size", host.RestoreBounds.Width + " " + host.RestoreBounds.Height); } catch (IOException) {} catch (UnauthorizedAccessException) {} };
    }

    private async Task CheckAll()
    {
        if (!measure.IsEnabled) return;
        measure.IsEnabled = false;
        try { await Task.WhenAll(RefreshLocation(), Measure()); }
        finally { measure.IsEnabled = true; }
    }

    private Task RefreshLocation()
    {
        if (locationRequest == null || locationRequest.IsCompleted) locationRequest = FetchLocation();
        return locationRequest;
    }

    private async Task FetchLocation()
    {

        try
        {
            const string url = "https://ipwho.is/?lang=ru&fields=success,city,country,country_code";
            var result = new JavaScriptSerializer().Deserialize<GeoResult>(await Geo.GetStringAsync(url));
            if (result == null || !result.success || String.IsNullOrWhiteSpace(result.country))
                throw new InvalidDataException();
            detected = result;
        }
        catch (Exception)
        {
            detected = null;
        }
        finally
        {
            RenderLocation();

        }
    }

    private void RenderLocation()
    {
        var parts = expected.Split(new[] { ',' }, 2).Select(x => x.Trim()).ToArray();
        bool wrong = false;
        if (detected == null)
        {
            location.Text = "Локация недоступна";
            locationStatus.Text = "Нажмите «Проверить»";
        }
        else
        {
            location.Text = String.IsNullOrWhiteSpace(detected.city)
                ? detected.country : detected.city + ", " + detected.country;
            bool cityKnown = !String.IsNullOrWhiteSpace(detected.city);
            string country = parts.Length == 2 ? parts[1] : "";
            if (MatchesCountry(parts[0])) country = parts[0];
            bool compareCountry = country.Length != 0;
            wrong = compareCountry ? !MatchesCountry(country) : cityKnown && parts[0].Length != 0
                && !Same(parts[0], detected.city);
            if (expected.Length == 0)
                locationStatus.Text = "Задайте ожидаемую локацию в ⚙";
            else if (wrong)
                locationStatus.Text = "Ожидалось: " + expected;
            else
                locationStatus.Text = compareCountry ? "Страна совпадает"
                    : cityKnown ? "Город совпадает" : "Город по IP недоступен";
        }
        location.ToolTip = location.Text;
        locationStatus.ToolTip = locationStatus.Text;
        location.Foreground = wrong ? ColorBrush("#A93D09") : Ink;
        if (wrong && flame.Visibility != Visibility.Visible)
        {
            var glow = new DropShadowEffect {
                Color = Color.FromRgb(255, 100, 34), BlurRadius = 15, ShadowDepth = 0
            };
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0.4, 0.95,
                new Duration(TimeSpan.FromMilliseconds(550))) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
            location.Effect = glow;
            fire.Visibility = Visibility.Visible;
            fire.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.60, 0.96, new Duration(TimeSpan.FromMilliseconds(740))) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
        }
        if (!wrong && fire.Visibility == Visibility.Visible)
        {
            fire.BeginAnimation(UIElement.OpacityProperty, null);
            fire.Visibility = Visibility.Collapsed;
            location.Effect = null;
        }
        flame.Visibility = wrong ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool MatchesCountry(string value)
    {
        if (Same(value, detected.country) || Same(value, detected.country_code)) return true;
        if (String.IsNullOrEmpty(detected.country_code)) return false;
        try
        {
            var region = new RegionInfo(detected.country_code);
            return Same(value, region.EnglishName) || Same(value, region.NativeName);
        }
        catch (ArgumentException) { return false; }
    }

    private static bool Same(string first, string second)
    {
        if (second == null) return false;
        var left = String.Join(" ", first.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)).Replace('ё', 'е').Replace('Ё', 'Е');
        var right = String.Join(" ", second.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)).Replace('ё', 'е').Replace('Ё', 'Е');
        return String.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private void EditExpected()
    {
        var dialog = new Window {
            Title = "Настройки", Topmost = true, Width = 390,
            SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false,
            Background = ColorBrush("#F6F6F8"), FontFamily = FontFamily
        };
        var panel = new StackPanel { Margin = new Thickness(16) };
        new WindowInteropHelper(dialog).Owner = host.Handle;
        dialog.Content = panel;
        var hint = Label("Ожидаемая локация", 14, Ink);
        panel.Children.Add(hint);
        hint = Label("Выберите страну из списка или введите название либо код (к примеру Нидерланы, NL). Подойдёт любой город этой страны. Город и страну можно указать через запятую в любом порядке. Один город сравнивается по названию. Пустое поле отключает сравнение.", 13, Muted);
        hint.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(hint);
        var input = new ComboBox { IsEditable = true, IsTextSearchEnabled = false, ItemsSource = Countries, Text = expected, MaxDropDownHeight = 270, Padding = new Thickness(5), Margin = new Thickness(0, 10, 0, 10) };
        panel.Children.Add(input);
        panel.Children.Add(Label("Непрозрачность окна", 14, Ink));
        double originalOpacity = windowAlpha;
        var opacity = new Slider { Minimum = 25, Maximum = 100, Value = windowAlpha * 100, Margin = new Thickness(0,8,0,12), AutoToolTipPlacement = System.Windows.Controls.Primitives.AutoToolTipPlacement.TopLeft, AutoToolTipPrecision = 0 };
        opacity.ValueChanged += (s,e) => ApplyOpacity(opacity.Value / 100);
        dialog.Closed += (s,e) => { if (dialog.DialogResult != true) ApplyOpacity(originalOpacity); };
        panel.Children.Add(opacity);
        var save = ActionButton("Сохранить", "Применить настройки");
        save.IsDefault = true;
        panel.Children.Add(save);
        save.Click += (s, e) => {
            var value = input.Text.Trim().TrimEnd(',').Trim();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Settings));
                File.WriteAllText(Settings, value, Encoding.UTF8);
                
                File.WriteAllText(ScaleSettings + ".opacity", windowAlpha.ToString(CultureInfo.InvariantCulture));
                expected = value;
                
                RenderLocation();
                dialog.DialogResult = true;
            }
            catch (Exception)
            {
                MessageBox.Show(dialog, "Не удалось сохранить настройку.", Title);
            }
        };
        dialog.Loaded += (s, e) => { var area = SystemParameters.WorkArea;
            dialog.Left = Math.Max(area.Left, Math.Min(host.Left, area.Right - dialog.ActualWidth));
            dialog.Top = host.Bottom + dialog.ActualHeight <= area.Bottom ? host.Bottom : Math.Max(area.Top, host.Top - dialog.ActualHeight);
            input.Focus(); };
        dialog.ShowDialog();
    }

    [DllImport("user32.dll", EntryPoint="GetWindowLongW")]
    private static extern int GetStyle(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongW")]
    private static extern int SetStyle(IntPtr hwnd, int index, int value);
    [DllImport("user32.dll", SetLastError=true)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint key, byte alpha, uint flags);
    [DllImport("user32.dll")]
    private static extern bool GetLayeredWindowAttributes(IntPtr hwnd, out uint key, out byte alpha, out uint flags);

    private void ApplyOpacity(double value)
    {
        host.Opacity = value;
        windowAlpha = value;
    }

    private static string ReadSetting(string path)
    {
        try { return File.ReadAllText(path, Encoding.UTF8).Trim(); }
        catch (IOException) { return ""; }
        catch (UnauthorizedAccessException) { return ""; }
    }

    private async Task Measure()
    {

        speedStatus.Text = "Идёт замер…";
        speedStatus.ToolTip = null;
        down.Text = up.Text = "—";
        try
        {
            down.Text = Rate(await MeasureDirection(false));
            up.Text = Rate(await MeasureDirection(true));
            speedStatus.Text = "Замер в " + DateTime.Now.ToString("HH:mm");
        }
        catch (Exception error)
        {
            speedStatus.Text = "Замер не завершён";
            speedStatus.ToolTip = error.Message;
        }

    }

    private static async Task<double> MeasureDirection(bool upload)
    {
        int sampleSize = upload ? 250000 : 500000;
        double estimate = await Transfer(upload, sampleSize);
        int bytes = (int)Math.Max(sampleSize, Math.Min(upload ? 10000000 : 25000000, estimate * 250000));
        return await Transfer(upload, bytes);
    }

    private static async Task<double> Transfer(bool upload, int bytes)
    {
        string url = "https://speed.cloudflare.com/" + (upload ? "__up?sample=" : "__down?bytes=" + bytes + "&sample=")
            + Guid.NewGuid().ToString("N");
        using (var request = new HttpRequestMessage(upload ? HttpMethod.Post : HttpMethod.Get, url))
        {
            if (upload) request.Content = new ByteArrayContent(new byte[bytes]);
            var watch = Stopwatch.StartNew();
            using (var response = await Speed.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                if (!upload)
                {
                    var payload = await response.Content.ReadAsByteArrayAsync();
                    if (payload.Length != bytes) throw new InvalidDataException("Неполный ответ сервера");
                }
                return bytes * 8.0 / 1000000 / Math.Max(0.001, watch.Elapsed.TotalSeconds);
            }
        }
    }

    private static string Rate(double value)
    {
        return value.ToString(value < 10 ? "0.0" : "0", CultureInfo.GetCultureInfo("ru-RU")) + " Мбит/с";
    }

    private static void AddMetric(Grid grid, int column, string title, TextBlock value)
    {
        var panel = new StackPanel();
        panel.Children.Add(Label(title, 12, Muted));
        panel.Children.Add(value);
        var tile = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(10), Padding = new Thickness(10, 5, 8, 5), Margin = new Thickness(column == 0 ? 0 : 3, 0, column == 0 ? 3 : 0, 0), Child = panel };
        value.FontWeight = FontWeights.SemiBold;
        Grid.SetColumn(tile, column);
        grid.Children.Add(tile);
    }

    private static TextBlock Label(string text, double size, Brush color)
    {
        return new TextBlock { Text = text, FontSize = size, Foreground = color };
    }

    private static Button ActionButton(string text, string tooltip)
    {
        var button = new Button {
            Content = text, ToolTip = tooltip, Foreground = Ink, Background = ColorBrush("#E6E7EB"),
            BorderBrush = ColorBrush("#DBDCE1"), Padding = new Thickness(7, 2, 7, 2),
            Margin = new Thickness(4, 0, 0, 0), MinWidth = 27, MinHeight = 24
            };
        bool primary = text.Length > 1;
        if (primary) { button.Background = ColorBrush("#245FC2"); button.Foreground = Brushes.White; }
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        template.VisualTree = border;
        var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Control.BackgroundProperty, ColorBrush(primary ? "#194FA8" : "#D7D9DF")));
        template.Triggers.Add(hover);
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.55));
        template.Triggers.Add(disabled);
        button.Template = template;
        return button;
    }

    private static Brush ColorBrush(string value)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
    }

    public sealed class GeoResult
    {
        public bool success { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string country_code { get; set; }
    }
}








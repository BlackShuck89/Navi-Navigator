using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace NaviNavigator;

internal sealed class BrowserForm : Form
{
    private const string PreviewTitle = "Navi Navigator — Technical Preview";
    private readonly BrowserSettings _settings = BrowserSettings.Load();
    private NaviSkin _skin;
    private readonly MenuStrip _menu = new();
    private readonly ToolStrip _toolbar = new();
    private readonly TabControl _tabs = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripTextBox _address = new();
    private readonly ToolStripStatusLabel _statusText = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _engineStatus = new("WebView2 test engine");
    private readonly ToolStripButton _back = new("Back");
    private readonly ToolStripButton _forward = new("Forward");
    private readonly ToolStripButton _reload = new("Reload");
    private readonly ToolStripMenuItem _bookmarksMenu = new("&Bookmarks");
    private readonly ToolStripMenuItem _skinsMenu = new("&Skins");
    private readonly Dictionary<TabPage, WebView2> _views = [];
    private readonly HashSet<TabPage> _homePages = [];
    private CoreWebView2Environment? _environment;

    private WebView2? CurrentView =>
        _tabs.SelectedTab is { } page && _views.TryGetValue(page, out var view) ? view : null;

    internal BrowserForm()
    {
        _skin = NaviSkin.Get(_settings.Skin);
        Text = PreviewTitle;
        Icon = NaviArt.MakeIcon();
        MinimumSize = new Size(720, 470);
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1180, 760);
        Font = new Font("Segoe UI", 9f);
        KeyPreview = true;

        BuildMenus();
        BuildToolbar();
        BuildTabs();

        _status.Dock = DockStyle.Bottom;
        _status.SizingGrip = true;
        _status.Items.Add(_statusText);
        _status.Items.Add(new ToolStripStatusLabel("◈"));
        _status.Items.Add(_engineStatus);
        _statusText.Text = "Starting Navi Navigator…";

        Controls.Add(_tabs);
        Controls.Add(_status);
        Controls.Add(_toolbar);
        Controls.Add(_menu);
        MainMenuStrip = _menu;
        _menu.Dock = DockStyle.Top;
        _toolbar.Dock = DockStyle.Top;
        _tabs.Dock = DockStyle.Fill;

        Resize += (_, _) => ResizeAddress();
        KeyDown += OnBrowserKeyDown;
        ApplySkin(_skin, save: false);
        Shown += async (_, _) => await InitializeBrowserAsync();
    }

    private void BuildMenus()
    {
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(new ToolStripMenuItem("&New Tab", null,
            async (_, _) => await OpenTabAsync(), Keys.Control | Keys.T));
        file.DropDownItems.Add(new ToolStripMenuItem("Close &Tab", null,
            (_, _) => CloseCurrentTab(), Keys.Control | Keys.W));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close()));

        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.Add(new ToolStripMenuItem("Copy Page &Address", null,
            (_, _) => CopyAddress()));
        edit.DropDownItems.Add(new ToolStripMenuItem("&Find in Page", null,
            (_, _) => CurrentView?.CoreWebView2?.OpenDefaultDownloadDialog(), Keys.None));
        // Find is enabled below via the WebView2 find shortcut (Ctrl+F) in page content.
        edit.DropDownItems.RemoveAt(edit.DropDownItems.Count - 1);

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(new ToolStripMenuItem("&Reload", null,
            (_, _) => ReloadPage(), Keys.F5));
        view.DropDownItems.Add(new ToolStripMenuItem("Open &Home", null,
            (_, _) => NavigateHome(), Keys.Alt | Keys.Home));
        view.DropDownItems.Add(new ToolStripSeparator());
        foreach (var skin in NaviSkin.All)
        {
            var chosen = skin;
            _skinsMenu.DropDownItems.Add(new ToolStripMenuItem(chosen.Name, null,
                (_, _) => ApplySkin(chosen)) { Tag = chosen.Id });
        }
        view.DropDownItems.Add(_skinsMenu);

        var go = new ToolStripMenuItem("&Go");
        go.DropDownItems.Add(new ToolStripMenuItem("&Back", null,
            (_, _) => GoBack(), Keys.Alt | Keys.Left));
        go.DropDownItems.Add(new ToolStripMenuItem("&Forward", null,
            (_, _) => GoForward(), Keys.Alt | Keys.Right));
        go.DropDownItems.Add(new ToolStripMenuItem("&Home", null,
            (_, _) => NavigateHome()));

        var tools = new ToolStripMenuItem("&Tools");
        tools.DropDownItems.Add(new ToolStripMenuItem("Open &Downloads Folder", null,
            (_, _) => OpenDownloadsFolder()));
        tools.DropDownItems.Add(new ToolStripMenuItem("Open Navi &Data Folder", null,
            (_, _) => OpenDataFolder()));

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(new ToolStripMenuItem("&About Navi Navigator", null,
            (_, _) => ShowAbout()));

        _menu.Items.AddRange([file, edit, view, go, _bookmarksMenu, tools, help]);
        RefreshBookmarksMenu();
    }

    private void BuildToolbar()
    {
        _toolbar.GripStyle = ToolStripGripStyle.Visible;
        _toolbar.Padding = new Padding(5, 4, 5, 4);
        _toolbar.ImageScalingSize = new Size(18, 18);
        _toolbar.Items.Add(new ToolStripLabel("◈ NAVI")
        {
            Name = "NaviBrandLabel",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            Margin = new Padding(4, 0, 13, 0)
        });

        _back.ToolTipText = "Go back (Alt+Left)";
        _forward.ToolTipText = "Go forward (Alt+Right)";
        _reload.ToolTipText = "Reload page (F5)";
        _toolbar.Items.Add(_back);
        _toolbar.Items.Add(_forward);
        _toolbar.Items.Add(_reload);
        _toolbar.Items.Add(new ToolStripButton("Home", null, (_, _) => NavigateHome()));
        _toolbar.Items.Add(new ToolStripSeparator());
        _back.Click += (_, _) => GoBack();
        _forward.Click += (_, _) => GoForward();
        _reload.Click += (_, _) => ReloadPage();
        _address.AutoSize = false;
        _address.Width = 440;
        _address.Name = "AddressBar";
        _address.ToolTipText = "Enter a URL or search the web (Ctrl+L)";
        _address.BorderStyle = BorderStyle.FixedSingle;
        _address.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                NavigateAddress(_address.Text);
                e.SuppressKeyPress = true;
            }
        };
        _toolbar.Items.Add(_address);
        _toolbar.Items.Add(new ToolStripButton("Go", null,
            (_, _) => NavigateAddress(_address.Text)));
        _toolbar.Items.Add(new ToolStripButton("+ Tab", null,
            async (_, _) => await OpenTabAsync()));
        _toolbar.Items.Add(new ToolStripButton("☆", null,
            (_, _) => AddBookmark()) { ToolTipText = "Bookmark this page (Ctrl+D)" });
        ResizeAddress();
    }

    private void BuildTabs()
    {
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.SizeMode = TabSizeMode.Fixed;
        _tabs.ItemSize = new Size(182, 30);
        _tabs.Padding = new Point(10, 3);
        _tabs.DrawItem += DrawTab;
        _tabs.SelectedIndexChanged += (_, _) => UpdateCurrentUi();
        _tabs.MouseDown += (_, e) =>
        {
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                var rect = _tabs.GetTabRect(i);
                if (!rect.Contains(e.Location)) continue;
                if (e.Button == MouseButtons.Middle ||
                    (e.Button == MouseButtons.Left && e.X > rect.Right - 27))
                {
                    CloseTab(_tabs.TabPages[i]);
                }
                break;
            }
        };
    }

    private void DrawTab(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _tabs.TabPages.Count) return;
        var rectangle = _tabs.GetTabRect(e.Index);
        bool selected = e.Index == _tabs.SelectedIndex;
        using var gradient = new LinearGradientBrush(
            rectangle,
            selected ? _skin.TabTop : _skin.Top,
            selected ? _skin.TabBottom : _skin.Bottom,
            LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(gradient, rectangle);
        using var borderPen = new Pen(_skin.Border);
        e.Graphics.DrawRectangle(borderPen, rectangle.X, rectangle.Y,
            rectangle.Width - 1, rectangle.Height - 1);
        if (selected)
        {
            using var accent = new Pen(_skin.AccentLight, 3);
            e.Graphics.DrawLine(accent, rectangle.X + 2, rectangle.Top + 1,
                rectangle.Right - 4, rectangle.Top + 1);
        }
        var textRectangle = new Rectangle(
            rectangle.Left + 10, rectangle.Top + 3, rectangle.Width - 39,
            rectangle.Height - 6);
        TextRenderer.DrawText(e.Graphics, _tabs.TabPages[e.Index].Text,
            Font, textRectangle, _skin.Foreground,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(e.Graphics, "×", Font,
            new Rectangle(rectangle.Right - 26, rectangle.Top + 3, 22, rectangle.Height - 6),
            _skin.Foreground, TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter);
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            string profile = Path.Combine(BrowserSettings.BaseDirectory, "WebView2");
            Directory.CreateDirectory(profile);
            _environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profile);
            await OpenTabAsync();
            _statusText.Text = "Ready — Navi Navigator technical preview";
        }
        catch (Exception ex)
        {
            _statusText.Text = "WebView2 initialization failed";
            MessageBox.Show(
                "Navi Navigator could not initialize the WebView2 runtime.\n\n" +
                ex.Message +
                "\n\nInstall Microsoft Edge WebView2 Runtime if it is missing. " +
                "This is an early test build and does not yet use Gecko.",
                PreviewTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OpenTabAsync(string? destination = null)
    {
        if (_environment is null) return;
        var page = new TabPage("New Tab") { Padding = new Padding(0), BackColor = Color.White };
        var browser = new WebView2 { Dock = DockStyle.Fill };
        page.Controls.Add(browser);
        _views.Add(page, browser);
        _tabs.TabPages.Add(page);
        _tabs.SelectedTab = page;

        try
        {
            await browser.EnsureCoreWebView2Async(_environment);
            if (page.IsDisposed) return;
            browser.CoreWebView2.Settings.AreDevToolsEnabled = true;
            browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            browser.CoreWebView2.DocumentTitleChanged += (_, _) =>
            {
                if (!page.IsDisposed)
                {
                    page.Text = TruncateTitle(browser.CoreWebView2.DocumentTitle);
                    _tabs.Invalidate();
                    if (_tabs.SelectedTab == page) UpdateCurrentUi();
                }
            };
            browser.CoreWebView2.HistoryChanged += (_, _) =>
            {
                if (_tabs.SelectedTab == page) UpdateCurrentUi();
            };
            browser.CoreWebView2.SourceChanged += (_, _) =>
            {
                if (_tabs.SelectedTab == page) UpdateCurrentUi();
            };
            browser.CoreWebView2.NavigationStarting += (_, e) =>
            {
                if (!e.Uri.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase))
                    _homePages.Remove(page);
                if (_tabs.SelectedTab == page) _statusText.Text = "Connecting…";
            };
            browser.CoreWebView2.NavigationCompleted += (_, e) =>
            {
                if (_tabs.SelectedTab == page)
                {
                    _statusText.Text = e.IsSuccess ? "Done" :
                        $"Navigation failed: {e.WebErrorStatus}";
                    UpdateCurrentUi();
                }
            };
            browser.CoreWebView2.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                string url = e.Uri;
                BeginInvoke(new Action(async () => await OpenTabAsync(url)));
            };
            browser.CoreWebView2.DownloadStarting += (_, e) =>
            {
                _statusText.Text = $"Downloading: {Path.GetFileName(e.ResultFilePath)}";
            };

            if (string.IsNullOrWhiteSpace(destination) || destination == "navi://home")
                ShowHome(page, browser);
            else
                browser.CoreWebView2.Navigate(destination);
            UpdateCurrentUi();
        }
        catch (Exception ex)
        {
            if (!page.IsDisposed)
            {
                page.Controls.Clear();
                page.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = "Web page initialization failed:\n" + ex.Message
                });
                page.Text = "Error";
                _statusText.Text = "Cannot open tab";
            }
        }
    }

    private static string TruncateTitle(string? title) =>
        string.IsNullOrWhiteSpace(title) ? "New Tab" :
            title.Length > 24 ? title[..22] + "…" : title;

    private void ShowHome(TabPage page, WebView2 view)
    {
        if (view.CoreWebView2 is null) return;
        _homePages.Add(page);
        page.Text = "Navi Portal";
        view.CoreWebView2.NavigateToString(HomeHtml);
        UpdateCurrentUi();
    }

    private void NavigateHome()
    {
        if (_tabs.SelectedTab is { } page && CurrentView is { } browser)
            ShowHome(page, browser);
    }

    private static string ResolveAddress(string text)
    {
        string entered = text.Trim();
        if (string.IsNullOrEmpty(entered)) return "navi://home";
        if (entered.Equals("navi://home", StringComparison.OrdinalIgnoreCase)) return "navi://home";
        if (Uri.TryCreate(entered, UriKind.Absolute, out var direct) &&
            (direct.Scheme == Uri.UriSchemeHttp || direct.Scheme == Uri.UriSchemeHttps))
            return direct.AbsoluteUri;
        if (!entered.Any(char.IsWhiteSpace) &&
            (entered.Contains('.') || entered.Equals("localhost", StringComparison.OrdinalIgnoreCase)) &&
            Uri.TryCreate("https://" + entered, UriKind.Absolute, out var guessed))
            return guessed.AbsoluteUri;
        return "https://duckduckgo.com/?q=" + Uri.EscapeDataString(entered);
    }

    private void NavigateAddress(string input)
    {
        string target = ResolveAddress(input);
        if (target == "navi://home")
        {
            NavigateHome();
            return;
        }
        if (CurrentView?.CoreWebView2 is { } web)
        {
            _homePages.Remove(_tabs.SelectedTab!);
            web.Navigate(target);
        }
    }

    private void GoBack()
    {
        if (CurrentView?.CoreWebView2 is { CanGoBack: true } web) web.GoBack();
    }

    private void GoForward()
    {
        if (CurrentView?.CoreWebView2 is { CanGoForward: true } web) web.GoForward();
    }

    private void ReloadPage()
    {
        if (CurrentView?.CoreWebView2 is { } web) web.Reload();
    }

    private void UpdateCurrentUi()
    {
        var current = CurrentView?.CoreWebView2;
        _back.Enabled = current?.CanGoBack == true;
        _forward.Enabled = current?.CanGoForward == true;
        if (current is null)
        {
            _address.Text = "";
            Text = PreviewTitle;
            return;
        }
        bool home = _tabs.SelectedTab is { } page && _homePages.Contains(page);
        _address.Text = home ? "navi://home" : (current.Source ?? "");
        Text = $"{(_tabs.SelectedTab?.Text ?? "New Tab")} - {PreviewTitle}";
    }

    private void CloseCurrentTab()
    {
        if (_tabs.SelectedTab is { } page) CloseTab(page);
    }

    private void CloseTab(TabPage page)
    {
        if (!_views.TryGetValue(page, out var browser)) return;
        _views.Remove(page);
        _homePages.Remove(page);
        _tabs.TabPages.Remove(page);
        page.Dispose();
        browser.Dispose();
        if (_tabs.TabPages.Count == 0) Close();
        else UpdateCurrentUi();
    }

    private void ResizeAddress()
    {
        _address.Width = Math.Max(195, ClientSize.Width - 550);
    }

    private void ApplySkin(NaviSkin skin, bool save = true)
    {
        _skin = skin;
        _settings.Skin = skin.Id;
        if (save) TrySave();
        var renderer = new ToolStripProfessionalRenderer(new NaviColorTable(skin));
        _menu.Renderer = renderer;
        _toolbar.Renderer = renderer;
        _status.Renderer = renderer;
        _menu.BackColor = skin.Top;
        _toolbar.BackColor = skin.Top;
        _status.BackColor = skin.Bottom;
        _menu.ForeColor = skin.Foreground;
        _toolbar.ForeColor = skin.Foreground;
        _status.ForeColor = skin.Foreground;
        _tabs.BackColor = skin.Bottom;
        _tabs.ForeColor = skin.Foreground;
        _statusText.ForeColor = skin.Foreground;
        _engineStatus.ForeColor = skin.Foreground;
        foreach (ToolStripItem item in _toolbar.Items)
        {
            item.ForeColor = skin.Foreground;
        }
        foreach (ToolStripItem item in _menu.Items)
        {
            item.ForeColor = skin.Foreground;
        }
        _address.BackColor = skin.Id == "fusion" ? Color.FromArgb(237, 249, 250) : Color.White;
        _address.ForeColor = Color.FromArgb(24, 47, 61);
        foreach (ToolStripMenuItem item in _skinsMenu.DropDownItems.OfType<ToolStripMenuItem>())
            item.Checked = Equals(item.Tag, skin.Id);
        _tabs.Invalidate();
        _menu.Invalidate();
        _toolbar.Invalidate();
    }

    private void RefreshBookmarksMenu()
    {
        _bookmarksMenu.DropDownItems.Clear();
        _bookmarksMenu.DropDownItems.Add(new ToolStripMenuItem("&Bookmark This Page", null,
            (_, _) => AddBookmark(), Keys.Control | Keys.D));
        _bookmarksMenu.DropDownItems.Add(new ToolStripSeparator());
        if (_settings.Bookmarks.Count == 0)
        {
            _bookmarksMenu.DropDownItems.Add(new ToolStripMenuItem("(No bookmarks yet)")
            {
                Enabled = false
            });
            return;
        }
        foreach (var bookmark in _settings.Bookmarks.ToArray())
        {
            var saved = bookmark;
            _bookmarksMenu.DropDownItems.Add(new ToolStripMenuItem(
                TruncateTitle(saved.Title), null, (_, _) => NavigateAddress(saved.Url)));
        }
    }

    private void AddBookmark()
    {
        if (CurrentView?.CoreWebView2 is not { } current ||
            (_tabs.SelectedTab is { } page && _homePages.Contains(page))) return;
        var url = current.Source;
        if (!Uri.TryCreate(url, UriKind.Absolute, out _)) return;
        if (_settings.Bookmarks.Any(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase)))
        {
            _statusText.Text = "Already bookmarked";
            return;
        }
        _settings.Bookmarks.Add(new Bookmark(
            string.IsNullOrWhiteSpace(current.DocumentTitle) ? url : current.DocumentTitle, url));
        TrySave();
        RefreshBookmarksMenu();
        _statusText.Text = "Bookmarked";
    }

    private void TrySave()
    {
        try { _settings.Save(); }
        catch (Exception ex)
        {
            _statusText.Text = "Settings could not be saved: " + ex.Message;
        }
    }

    private void CopyAddress()
    {
        if (CurrentView?.CoreWebView2 is { } current)
        {
            var url = current.Source;
            if (!string.IsNullOrWhiteSpace(url)) Clipboard.SetText(url);
        }
    }

    private void OpenDownloadsFolder()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        if (Directory.Exists(path)) OpenFolder(path);
    }

    private void OpenDataFolder()
    {
        Directory.CreateDirectory(BrowserSettings.BaseDirectory);
        OpenFolder(BrowserSettings.BaseDirectory);
    }

    private static void OpenFolder(string path) =>
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"")
        { UseShellExecute = true });

    private void ShowAbout() => MessageBox.Show(
        "Navi Navigator 0.0.1 — Windows technical preview\n\n" +
        "Netscape 7/8-inspired desktop interface, original Navi compass branding.\n\n" +
        "Renderer: Microsoft WebView2 (Chromium), NOT Gecko.\n" +
        "This is an interim test shell, not a production security-audited browser.\n\n" +
        "Navi Navigator is not affiliated with Netscape, Mozilla or Microsoft.",
        "About Navi Navigator", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void OnBrowserKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.L)
        {
            _address.Focus();
            _address.SelectAll();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.T)
        {
            _ = OpenTabAsync();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.W)
        {
            CloseCurrentTab();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.D)
        {
            AddBookmark();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F5)
        {
            ReloadPage();
            e.Handled = true;
        }
        else if (e.Alt && e.KeyCode == Keys.Left)
        {
            GoBack();
            e.Handled = true;
        }
        else if (e.Alt && e.KeyCode == Keys.Right)
        {
            GoForward();
            e.Handled = true;
        }
    }

    private const string HomeHtml = """
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>Navi Portal</title>
        <style>
          :root { color-scheme: dark; }
          body { margin:0; min-height:100vh; background: radial-gradient(circle at 55% 15%,#2c6680,#142d43 55%,#0a1a2d);
                 color:#dff5f5; font: 16px 'Segoe UI',Tahoma,sans-serif; }
          main { max-width:790px; margin: auto; padding:8vh 30px; text-align:center; }
          .compass { width:104px; height:104px; margin:0 auto 15px; border-radius:50%; border:6px solid #9bd9d9;
                     background: radial-gradient(circle,#32a9aa 50%,#12636a); display:grid; place-items:center;
                     box-shadow:0 4px 30px #071a2d; font-size:73px; color:#fff; }
          h1 {font-size:37px; margin:7px 0; font-weight:650; letter-spacing:.3px}
          p {color:#b8d8e1}
          section {display:grid;grid-template-columns:repeat(auto-fit,minmax(175px,1fr));gap:15px;margin-top:38px}
          a {display:block; padding:22px 17px; text-decoration:none; border:1px solid #578d9b; border-radius:10px;
             color:#e7fafb; background:linear-gradient(#33667f,#24445f); font-weight:600}
          a:hover {background:linear-gradient(#4b8d9b,#256e77);border-color:#83dedb}
          footer{margin-top:47px;font-size:12px;color:#92b6c2}
        </style>
        </head>
        <body><main><div class="compass">✦</div>
        <h1>Navi Navigator</h1>
        <p>Explore the web again. Classic spirit, contemporary technology.</p>
        <section>
          <a href="https://en.wikipedia.org/">Wikipedia</a>
          <a href="https://archive.org/">Internet Archive</a>
          <a href="https://www.mozilla.org/">Mozilla</a>
          <a href="https://github.com/BlackShuck89/Navi-Navigator">Project Repository</a>
        </section>
        <footer>0.0.1 technical preview · WebView2 prototype · Gecko browser in development</footer>
        </main></body></html>
        """;
}

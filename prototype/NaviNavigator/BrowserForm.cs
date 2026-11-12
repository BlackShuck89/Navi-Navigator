using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace NaviNavigator;

internal sealed class BrowserForm : Form
{
    private const string PreviewTitle = "Navi Navigator — Technical Preview 0.0.5";
    private readonly BrowserSettings _settings = BrowserSettings.Load();
    private readonly SkinCatalog _catalog = new();
    private NaviSkin _skin;
    private readonly MenuStrip _menu = new();
    private readonly ToolStrip _toolbar = new();
    private readonly ToolStrip _locationBar = new();
    private readonly ToolStrip _personalBar = new();
    private readonly Dictionary<string, ToolStripButton> _iconButtons = new(StringComparer.Ordinal);
    private readonly TabControl _tabs = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripTextBox _address = new();
    private readonly ToolStripStatusLabel _statusText = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _engineStatus = new("WebView2 test engine");
    private readonly ToolStripButton _back = new("Back");
    private readonly ToolStripButton _forward = new("Forward");
    private readonly ToolStripButton _reload = new("Reload");
    private readonly ToolStripButton _stop = new("Stop");
    private readonly ToolStripButton _home = new("Home");
    private readonly ToolStripButton _go = new("Go");
    private readonly ToolStripButton _newTab = new("New Tab");
    private readonly ToolStripButton _bookmark = new("Bookmark");
    private readonly ToolStripButton _brand = new("NAVI");
    private readonly ToolStripMenuItem _bookmarksMenu = new("&Bookmarks");
    private readonly ToolStripMenuItem _skinsMenu = new("&Skins");
    private readonly Dictionary<TabPage, WebView2> _views = [];
    private readonly HashSet<TabPage> _homePages = [];
    private readonly HashSet<TabPage> _loadingTabs = [];
    private readonly List<HistoryVisit> _history = BrowserStateStore.LoadHistory();
    private readonly Stack<string> _recentlyClosed = new();
    private readonly System.Windows.Forms.Timer _sessionSaveTimer = new() { Interval = 1400 };
    private bool _restoringSession;
    private CoreWebView2Environment? _environment;

    private WebView2? CurrentView =>
        _tabs.SelectedTab is { } page && _views.TryGetValue(page, out var view) ? view : null;

    internal BrowserForm()
    {
        _skin = _catalog.Get(_settings.Skin);
        Text = PreviewTitle;
        Icon = NaviArt.MakeIcon();
        MinimumSize = new Size(720, 470);
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1180, 760);
        Font = new Font("Segoe UI", 9f);
        KeyPreview = true;

        BuildMenus();
        BuildToolbar();
        BuildLocationBar();
        BuildPersonalBar();
        BuildTabs();

        _status.Dock = DockStyle.Bottom;
        _status.SizingGrip = true;
        _status.Items.Add(_statusText);
        _status.Items.Add(new ToolStripStatusLabel("◈"));
        _status.Items.Add(_engineStatus);
        _statusText.Text = "Starting Navi Navigator…";

        Controls.Add(_tabs);
        Controls.Add(_status);
        Controls.Add(_personalBar);
        Controls.Add(_locationBar);
        Controls.Add(_toolbar);
        Controls.Add(_menu);
        MainMenuStrip = _menu;
        _menu.Dock = DockStyle.Top;
        _toolbar.Dock = DockStyle.Top;
        _locationBar.Dock = DockStyle.Top;
        _personalBar.Dock = DockStyle.Top;
        _tabs.Dock = DockStyle.Fill;

        _sessionSaveTimer.Tick += (_, _) =>
        {
            _sessionSaveTimer.Stop();
            PersistSession();
        };
        FormClosing += (_, _) =>
        {
            _sessionSaveTimer.Stop();
            PersistSession();
            PersistHistory();
        };
        Resize += (_, _) => ResizeAddress();
        _locationBar.SizeChanged += (_, _) => ResizeAddress();
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
        file.DropDownItems.Add(new ToolStripMenuItem("Reopen Closed &Tab", null,
            (_, _) => ReopenClosedTab(), Keys.Control | Keys.Shift | Keys.T));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close()));

        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.Add(new ToolStripMenuItem("Copy Page &Address", null,
            (_, _) => CopyAddress()));
        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(new ToolStripMenuItem("&Reload", null,
            (_, _) => ReloadPage(), Keys.F5));
        view.DropDownItems.Add(new ToolStripMenuItem("Open &Home", null,
            (_, _) => NavigateHome(), Keys.Alt | Keys.Home));
        view.DropDownItems.Add(new ToolStripSeparator());

        var toolbars = new ToolStripMenuItem("&Toolbars");
        var personalToggle = new ToolStripMenuItem("&Personal Toolbar", null, (_, _) =>
        {
            _personalBar.Visible = !_personalBar.Visible;
            _settings.ShowPersonalBar = _personalBar.Visible;
            TrySave();
        }) { CheckOnClick = false };
        var statusToggle = new ToolStripMenuItem("&Status Bar", null, (_, _) =>
        {
            _status.Visible = !_status.Visible;
            _settings.ShowStatusBar = _status.Visible;
            TrySave();
        }) { CheckOnClick = false };
        var labelsToggle = new ToolStripMenuItem("Show &Button Labels", null, (_, _) =>
        {
            bool current = _settings.ShowToolbarLabels ?? _skin.ShowButtonLabels;
            _settings.ShowToolbarLabels = !current;
            ApplySkin(_skin);
        }) { CheckOnClick = false };
        toolbars.DropDownItems.Add(personalToggle);
        toolbars.DropDownItems.Add(statusToggle);
        toolbars.DropDownItems.Add(labelsToggle);
        toolbars.DropDownOpening += (_, _) =>
        {
            personalToggle.Checked = _personalBar.Visible;
            statusToggle.Checked = _status.Visible;
            labelsToggle.Checked = _settings.ShowToolbarLabels ?? _skin.ShowButtonLabels;
        };
        view.DropDownItems.Add(toolbars);
        view.DropDownItems.Add(new ToolStripSeparator());
        view.DropDownItems.Add(_skinsMenu);
        RebuildSkinMenu();

        var go = new ToolStripMenuItem("&Go");
        go.DropDownItems.Add(new ToolStripMenuItem("&Back", null,
            (_, _) => GoBack(), Keys.Alt | Keys.Left));
        go.DropDownItems.Add(new ToolStripMenuItem("&Forward", null,
            (_, _) => GoForward(), Keys.Alt | Keys.Right));
        go.DropDownItems.Add(new ToolStripMenuItem("&Home", null,
            (_, _) => NavigateHome()));
        go.DropDownItems.Add(new ToolStripMenuItem("&History", null,
            (_, _) => OpenHistory(), Keys.Control | Keys.H));

        var tools = new ToolStripMenuItem("&Tools");
        tools.DropDownItems.Add(new ToolStripMenuItem("Open &Downloads Folder", null,
            (_, _) => OpenDownloadsFolder()));
        tools.DropDownItems.Add(new ToolStripMenuItem("Open Navi &Data Folder", null,
            (_, _) => OpenDataFolder()));
        tools.DropDownItems.Add(new ToolStripMenuItem("Open &Skins Folder", null,
            (_, _) => OpenSkinFolder()));
        tools.DropDownItems.Add(new ToolStripSeparator());
        tools.DropDownItems.Add(new ToolStripMenuItem("Manage Browsing &History", null,
            (_, _) => OpenHistory()));
        var sessionToggle = new ToolStripMenuItem("&Restore Previous Session", null,
            (_, _) =>
            {
                _settings.RestorePreviousSession = !_settings.RestorePreviousSession;
                TrySave();
            });
        sessionToggle.CheckOnClick = false;
        tools.DropDownItems.Add(sessionToggle);
        tools.DropDownOpening += (_, _) =>
            sessionToggle.Checked = _settings.RestorePreviousSession;

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(new ToolStripMenuItem("&About Navi Navigator", null,
            (_, _) => ShowAbout()));

        _menu.Items.AddRange([file, edit, view, go, _bookmarksMenu, tools, help]);
        RefreshBookmarksMenu();
    }

    private void RebuildSkinMenu()
    {
        _skinsMenu.DropDownItems.Clear();
        foreach (var skin in _catalog.All)
        {
            var selected = skin;
            _skinsMenu.DropDownItems.Add(new ToolStripMenuItem(
                selected.Name, null, (_, _) => ApplySkin(selected)) { Tag = selected.Id });
        }
        _skinsMenu.DropDownItems.Add(new ToolStripSeparator());
        _skinsMenu.DropDownItems.Add(new ToolStripMenuItem("Open Skins Folder...", null,
            (_, _) => OpenSkinFolder()));
        _skinsMenu.DropDownItems.Add(new ToolStripMenuItem("Reload Skin Packages", null, (_, _) =>
        {
            _catalog.Reload();
            RebuildSkinMenu();
            ApplySkin(_catalog.Get(_settings.Skin));
            _statusText.Text = _catalog.Warnings.Count == 0
                ? "Skin packages reloaded" : $"{_catalog.Warnings.Count} skin packages skipped";
        }));
    }

    private ToolStripButton AddButton(ToolStrip destination, ToolStripButton button,
        string iconKey, string toolTip, EventHandler click)
    {
        button.Name = "NaviButton_" + iconKey;
        button.ToolTipText = toolTip;
        button.Click += click;
        _iconButtons[iconKey] = button;
        destination.Items.Add(button);
        return button;
    }

    private void BuildToolbar()
    {
        _toolbar.GripStyle = ToolStripGripStyle.Visible;
        _toolbar.Padding = new Padding(7, 3, 7, 3);
        _toolbar.CanOverflow = true;

        AddButton(_toolbar, _back, "back", "Back (Alt+Left)", (_, _) => GoBack());
        AddButton(_toolbar, _forward, "forward", "Forward (Alt+Right)", (_, _) => GoForward());
        AddButton(_toolbar, _reload, "reload", "Reload (F5)", (_, _) => ReloadPage());
        AddButton(_toolbar, _stop, "stop", "Stop loading", (_, _) =>
            CurrentView?.CoreWebView2?.Stop());
        AddButton(_toolbar, _home, "home", "Navi Portal (Alt+Home)", (_, _) => NavigateHome());
        _toolbar.Items.Add(new ToolStripSeparator());
        AddButton(_toolbar, _newTab, "tab", "New Tab (Ctrl+T)",
            async (_, _) => await OpenTabAsync());
        AddButton(_toolbar, _bookmark, "bookmark", "Bookmark page (Ctrl+D)",
            (_, _) => AddBookmark());

        _toolbar.Items.Add(new ToolStripSeparator());
        _brand.Alignment = ToolStripItemAlignment.Right;
        _brand.Font = new Font("Trebuchet MS", 10f, FontStyle.Bold);
        AddButton(_toolbar, _brand, "compass", "Navi Navigator Home", (_, _) => NavigateHome());
        _brand.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
    }

    private void BuildLocationBar()
    {
        _locationBar.GripStyle = ToolStripGripStyle.Visible;
        _locationBar.Padding = new Padding(7, 2, 7, 2);
        _locationBar.Items.Add(new ToolStripLabel("Location:"));
        _address.AutoSize = false;
        _address.Width = 450;
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
        _locationBar.Items.Add(_address);
        AddButton(_locationBar, _go, "go", "Navigate",
            (_, _) => NavigateAddress(_address.Text));
        ResizeAddress();
    }

    private void BuildPersonalBar()
    {
        _personalBar.GripStyle = ToolStripGripStyle.Visible;
        _personalBar.Padding = new Padding(6, 1, 6, 1);
        RefreshPersonalBar();
    }

    private void RefreshPersonalBar()
    {
        _personalBar.SuspendLayout();
        try
        {
            _personalBar.Items.Clear();
            _personalBar.Items.Add(new ToolStripLabel("Bookmarks  ▸")
            {
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ToolTipText = "Personal toolbar"
            });
            _personalBar.Items.Add(new ToolStripSeparator());
            var portal = new ToolStripButton("Navi Portal", null, (_, _) => NavigateHome());
            _personalBar.Items.Add(portal);
            foreach (var bookmark in _settings.Bookmarks.Take(9))
            {
                var item = bookmark;
                _personalBar.Items.Add(new ToolStripButton(TruncateTitle(item.Title), null,
                    (_, _) => NavigateAddress(item.Url)) { ToolTipText = item.Url });
            }
            if (_settings.Bookmarks.Count == 0)
            {
                _personalBar.Items.Add(new ToolStripButton("Internet Archive", null,
                    (_, _) => NavigateAddress("https://archive.org/")));
                _personalBar.Items.Add(new ToolStripButton("Wikipedia", null,
                    (_, _) => NavigateAddress("https://en.wikipedia.org/")));
            }
        }
        finally { _personalBar.ResumeLayout(); }
    }

    private void OpenSkinFolder()
    {
        _catalog.EnsureUserDirectory();
        OpenFolder(SkinCatalog.UserSkinDirectory);
    }

    private void BuildTabs()
    {
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.SizeMode = TabSizeMode.Fixed;
        _tabs.ItemSize = new Size(206, 34);
        _tabs.Padding = new Point(10, 3);
        _tabs.DrawItem += DrawTab;
        _tabs.SelectedIndexChanged += (_, _) =>
        {
            UpdateCurrentUi();
            QueueSessionSave();
        };
        _tabs.MouseDown += (_, e) =>
        {
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                var rect = _tabs.GetTabRect(i);
                if (!rect.Contains(e.Location)) continue;
                if (e.Button == MouseButtons.Right)
                {
                    _tabs.SelectedIndex = i;
                    ShowTabContextMenu(_tabs.TabPages[i], e.Location);
                }
                else if (e.Button == MouseButtons.Middle ||
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
        if (rectangle.Width < 8 || rectangle.Height < 8) return;
        bool selected = e.Index == _tabs.SelectedIndex;
        bool fusion = _skin.Id == "fusion";
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = new GraphicsPath();
        if (_skin.TabShape == "sloped")
        {
            path.AddPolygon(new Point[] {
                new(rectangle.Left, rectangle.Bottom - 1),
                new(rectangle.Left + 11, rectangle.Top + 2),
                new(rectangle.Left + 18, rectangle.Top),
                new(rectangle.Right - 9, rectangle.Top),
                new(rectangle.Right, rectangle.Bottom - 1)
            });
        }
        else
        {
            using var outline = NaviChromeRenderer.RoundedRectangle(
                new Rectangle(rectangle.Left, rectangle.Top + 1,
                    rectangle.Width - 1, rectangle.Height - 1),
                _skin.TabShape == "square" ? 1 : _skin.CornerRadius);
            path.AddPath(outline, false);
        }

        using var gradient = new LinearGradientBrush(
            rectangle,
            selected ? _skin.TabTopColor : _skin.TopColor,
            selected ? _skin.TabBottomColor : _skin.BottomColor,
            LinearGradientMode.Vertical);
        e.Graphics.FillPath(gradient, path);

        if (selected && _skin.SurfaceEffect == "gloss")
        {
            var shine = new Rectangle(rectangle.Left + 2, rectangle.Top + 2,
                Math.Max(1, rectangle.Width - 4), Math.Max(1, rectangle.Height / 2 - 3));
            using var glimmer = new LinearGradientBrush(shine,
                Color.FromArgb(fusion ? 36 : 135, Color.White),
                Color.FromArgb(4, Color.White), LinearGradientMode.Vertical);
            var oldClip = e.Graphics.Clip;
            e.Graphics.SetClip(path);
            e.Graphics.FillRectangle(glimmer, shine);
            e.Graphics.Clip = oldClip;
        }

        using var borderPen = new Pen(_skin.BorderColor);
        e.Graphics.DrawPath(borderPen, path);
        if (selected)
        {
            using var accent = new Pen(_skin.AccentLightColor, 2.4f);
            e.Graphics.DrawLine(accent, rectangle.Left + 17, rectangle.Top + 2,
                rectangle.Right - 12, rectangle.Top + 2);
        }
        var iconRect = new Rectangle(rectangle.Left + 10,
            rectangle.Top + (rectangle.Height - 17) / 2, 17, 17);
        if (selected || fusion)
        {
            using var icon = NaviArt.MakeButtonIcon("compass", _skin, 17);
            e.Graphics.DrawImage(icon, iconRect);
        }
        var textRectangle = new Rectangle(
            rectangle.Left + (selected || fusion ? 31 : 14), rectangle.Top + 3,
            rectangle.Width - (selected || fusion ? 68 : 51), rectangle.Height - 6);
        TextRenderer.DrawText(e.Graphics, _tabs.TabPages[e.Index].Text, Font,
            textRectangle, _skin.ForegroundColor,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(e.Graphics, "×", Font,
            new Rectangle(rectangle.Right - 28, rectangle.Top + 3, 22, rectangle.Height - 6),
            _skin.ForegroundColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            string profile = Path.Combine(BrowserSettings.BaseDirectory, "WebView2");
            Directory.CreateDirectory(profile);
            _environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profile);
            var saved = _settings.RestorePreviousSession
                ? BrowserStateStore.LoadSession() : null;
            if (saved is { Tabs.Count: > 0 })
            {
                _restoringSession = true;
                try
                {
                    foreach (var address in saved.Tabs)
                        await OpenTabAsync(address);
                    if (_tabs.TabCount > 0)
                        _tabs.SelectedIndex = Math.Clamp(saved.SelectedIndex, 0, _tabs.TabCount - 1);
                }
                finally { _restoringSession = false; }
            }
            else
            {
                await OpenTabAsync();
            }
            QueueSessionSave();
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
                    page.Text = _homePages.Contains(page) ? "Navi Portal" :
                        TruncateTitle(browser.CoreWebView2.DocumentTitle);
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
                QueueSessionSave();
            };
            browser.CoreWebView2.NavigationStarting += (_, e) =>
            {
                if (!e.Uri.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase))
                    _homePages.Remove(page);
                _loadingTabs.Add(page);
                if (_tabs.SelectedTab == page)
                {
                    _statusText.Text = "Connecting…";
                    UpdateCurrentUi();
                }
            };
            browser.CoreWebView2.NavigationCompleted += (_, e) =>
            {
                _loadingTabs.Remove(page);
                if (!page.IsDisposed && e.IsSuccess && !_homePages.Contains(page))
                    RecordHistory(browser.CoreWebView2.Source,
                        browser.CoreWebView2.DocumentTitle);
                QueueSessionSave();
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
                if (BrowserStateStore.IsWebAddress(url))
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
            QueueSessionSave();
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
        view.CoreWebView2.NavigateToString(NaviPortal.BuildHtml(_skin));
        UpdateCurrentUi();
        QueueSessionSave();
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
        _stop.Enabled = _tabs.SelectedTab is { } active && _loadingTabs.Contains(active);
        if (current is null)
        {
            _address.Text = "";
            Text = PreviewTitle;
            return;
        }
        bool home = _tabs.SelectedTab is { } page && (_homePages.Contains(page) ||
            (page.Text == "Navi Portal" && current.Source == "about:blank"));
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
        string location = GetTabLocation(page);
        if (location == "navi://home" || BrowserStateStore.IsWebAddress(location))
        {
            _recentlyClosed.Push(location);
            // Keep at most 10 recently closed tabs.
            if (_recentlyClosed.Count > 10)
            {
                var keep = _recentlyClosed.Take(10).ToArray();
                _recentlyClosed.Clear();
                for (int i = keep.Length - 1; i >= 0; i--) _recentlyClosed.Push(keep[i]);
            }
        }
        _views.Remove(page);
        _homePages.Remove(page);
        _loadingTabs.Remove(page);
        _tabs.TabPages.Remove(page);
        page.Dispose();
        browser.Dispose();
        if (_tabs.TabPages.Count == 0) Close();
        else
        {
            UpdateCurrentUi();
            QueueSessionSave();
        }
    }

    private void ResizeAddress()
    {
        if (_locationBar.IsDisposed) return;
        // Retain navigation controls at smaller window sizes.
        _address.Width = Math.Max(195, _locationBar.ClientSize.Width - 185);
    }

    private void ApplySkin(NaviSkin skin, bool save = true)
    {
        _skin = skin;
        _settings.Skin = skin.Id;
        if (save) TrySave();

        var renderer = new NaviChromeRenderer(skin);
        _menu.Renderer = renderer;
        _toolbar.Renderer = renderer;
        _locationBar.Renderer = renderer;
        _personalBar.Renderer = renderer;
        _status.Renderer = renderer;

        _menu.BackColor = skin.TopColor;
        _toolbar.BackColor = skin.TopColor;
        _locationBar.BackColor = skin.TopColor;
        _personalBar.BackColor = skin.TopColor;
        _status.BackColor = skin.BottomColor;
        _menu.ForeColor = skin.ForegroundColor;
        _toolbar.ForeColor = skin.ForegroundColor;
        _locationBar.ForeColor = skin.ForegroundColor;
        _personalBar.ForeColor = skin.ForegroundColor;
        _status.ForeColor = skin.ForegroundColor;
        _tabs.BackColor = skin.BottomColor;
        _tabs.ForeColor = skin.ForegroundColor;
        _statusText.ForeColor = skin.ForegroundColor;
        _engineStatus.ForeColor = skin.ForegroundColor;

        _toolbar.AutoSize = false;
        _toolbar.Height = skin.ToolbarHeight;
        _toolbar.ImageScalingSize = new Size(skin.IconSize, skin.IconSize);
        _locationBar.ImageScalingSize = new Size(19, 19);
        _personalBar.ImageScalingSize = new Size(17, 17);
        _locationBar.AutoSize = false;
        _locationBar.Height = 32;
        _personalBar.AutoSize = false;
        _personalBar.Height = 29;
        _tabs.ItemSize = new Size(skin.TabWidth, skin.TabHeight);
        _tabs.Padding = new Point(skin.TabShape == "sloped" ? 13 : 6, 2);
        _menu.Font = new Font(skin.Id == "classic" ? "Tahoma" : "Segoe UI", 9f);
        _toolbar.Font = new Font(skin.Id == "classic" ? "Tahoma" : "Segoe UI", 9f);
        _locationBar.Font = _toolbar.Font;
        _personalBar.Font = _toolbar.Font;

        foreach (ToolStripItem item in _toolbar.Items)
            item.ForeColor = skin.ForegroundColor;
        foreach (ToolStripItem item in _locationBar.Items)
            item.ForeColor = skin.ForegroundColor;
        foreach (ToolStripItem item in _personalBar.Items)
            item.ForeColor = skin.ForegroundColor;
        foreach (ToolStripMenuItem top in _menu.Items.OfType<ToolStripMenuItem>())
        {
            top.ForeColor = skin.ForegroundColor;
            ApplyDropdownColors(top, skin.MenuForegroundColor);
        }

        bool labels = _settings.ShowToolbarLabels ?? skin.ShowButtonLabels;
        foreach (var (key, button) in _iconButtons)
        {
            var old = button.Image;
            button.Image = NaviArt.MakeButtonIcon(key, skin,
                key == "compass" ? Math.Min(32, skin.IconSize + 3) : skin.IconSize);
            if (old is not null) old.Dispose();
            button.ImageScaling = ToolStripItemImageScaling.None;
            button.AutoSize = true;
            button.DisplayStyle = key == "compass" ?
                ToolStripItemDisplayStyle.ImageAndText :
                (labels ? ToolStripItemDisplayStyle.ImageAndText : ToolStripItemDisplayStyle.Image);
            button.TextImageRelation = TextImageRelation.ImageAboveText;
            button.ForeColor = skin.ForegroundColor;
            button.Margin = new Padding(2, 0, 2, 0);
        }
        _brand.TextImageRelation = TextImageRelation.ImageBeforeText;
        _brand.ToolTipText = "Navi Navigator";
        _address.BackColor = Color.FromArgb(246, 251, 251);
        _address.ForeColor = Color.FromArgb(24, 47, 61);

        foreach (ToolStripMenuItem item in _skinsMenu.DropDownItems.OfType<ToolStripMenuItem>())
            item.Checked = Equals(item.Tag, skin.Id);

        _personalBar.Visible = _settings.ShowPersonalBar;
        _status.Visible = _settings.ShowStatusBar;
        _tabs.Invalidate();
        _menu.Invalidate();
        _toolbar.Invalidate();
        _locationBar.Invalidate();
        _personalBar.Invalidate();

        // Update the local start page immediately when switching skins.
        foreach (var page in _homePages.ToArray())
        {
            if (!page.IsDisposed && _views.TryGetValue(page, out var browser) &&
                browser.CoreWebView2 is not null)
                ShowHome(page, browser);
        }
    }

    private static void ApplyDropdownColors(ToolStripMenuItem parent, Color color)
    {
        foreach (ToolStripItem child in parent.DropDownItems)
        {
            child.ForeColor = color;
            if (child is ToolStripMenuItem menu)
                ApplyDropdownColors(menu, color);
        }
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
        RefreshPersonalBar();
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

    private async void ShowAbout()
    {
        if (_environment is null)
        {
            MessageBox.Show("Navi Navigator is still initializing.", "About Navi Navigator",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var dialog = new Form
        {
            Text = "About Navi Navigator",
            Icon = Icon,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            ShowInTaskbar = false,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(730, 510),
            BackColor = _skin.TopColor
        };
        var aboutBrowser = new WebView2 { Dock = DockStyle.Fill };
        dialog.Controls.Add(aboutBrowser);
        dialog.Show(this);

        try
        {
            await aboutBrowser.EnsureCoreWebView2Async(_environment);
            if (dialog.IsDisposed || aboutBrowser.IsDisposed)
                return;
            aboutBrowser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            aboutBrowser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            aboutBrowser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            aboutBrowser.CoreWebView2.NewWindowRequested += (_, e) => e.Handled = true;
            aboutBrowser.CoreWebView2.NavigationStarting += (_, e) =>
            {
                // Only the locally generated HTML document is permitted in About.
                if (e.Uri != "about:blank") e.Cancel = true;
            };
            aboutBrowser.CoreWebView2.NavigateToString(NaviPortal.BuildAboutHtml(_skin));
        }
        catch (Exception ex)
        {
            if (!dialog.IsDisposed)
            {
                aboutBrowser.Dispose();
                dialog.Controls.Clear();
                dialog.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = "Navi Navigator 0.0.5 — Technical Preview\n\n" +
                           "About page could not be displayed:\n" + ex.Message
                });
            }
        }
    }

    private void OnBrowserKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.L)
        {
            _address.Focus();
            _address.SelectAll();
            e.Handled = true;
        }
        else if (e.Control && e.Shift && e.KeyCode == Keys.T)
        {
            ReopenClosedTab();
            e.Handled = true;
        }
        else if (e.Control && !e.Shift && e.KeyCode == Keys.T)
        {
            _ = OpenTabAsync();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.H)
        {
            OpenHistory();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.Tab)
        {
            SelectAdjacentTab(e.Shift ? -1 : 1);
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

    private void QueueSessionSave()
    {
        if (_restoringSession || _environment is null || IsDisposed) return;
        _sessionSaveTimer.Stop();
        _sessionSaveTimer.Start();
    }

    private string GetTabLocation(TabPage page)
    {
        if (_homePages.Contains(page)) return "navi://home";
        if (_views.TryGetValue(page, out var browser) &&
            browser.CoreWebView2 is { } web &&
            BrowserStateStore.IsWebAddress(web.Source))
            return web.Source;
        return "navi://home";
    }

    private void PersistSession()
    {
        if (_restoringSession || _environment is null) return;
        try
        {
            var locations = _tabs.TabPages.Cast<TabPage>()
                .Select(GetTabLocation).ToList();
            BrowserStateStore.SaveSession(locations, _tabs.SelectedIndex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!IsDisposed) _statusText.Text = "Could not save tab session: " + ex.Message;
        }
    }

    private void PersistHistory()
    {
        try { BrowserStateStore.SaveHistory(_history); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!IsDisposed) _statusText.Text = "Could not save history: " + ex.Message;
        }
    }

    private void RecordHistory(string? address, string? title)
    {
        if (!BrowserStateStore.IsWebAddress(address)) return;
        string safeTitle = string.IsNullOrWhiteSpace(title) ? address! : title.Trim();
        _history.Insert(0, new HistoryVisit(
            safeTitle.Length > 400 ? safeTitle[..400] : safeTitle,
            address!, DateTimeOffset.UtcNow));
        if (_history.Count > 2000) _history.RemoveRange(2000, _history.Count - 2000);
        PersistHistory();
    }

    private void OpenHistory()
    {
        using var window = new HistoryWindow(_history,
            url => { _ = OpenTabAsync(url); },
            PersistHistory);
        window.ShowDialog(this);
    }

    private void ReopenClosedTab()
    {
        if (_recentlyClosed.Count == 0)
        {
            _statusText.Text = "No recently closed tabs";
            return;
        }
        _ = OpenTabAsync(_recentlyClosed.Pop());
    }

    private void SelectAdjacentTab(int direction)
    {
        if (_tabs.TabCount == 0) return;
        _tabs.SelectedIndex = (_tabs.SelectedIndex + direction + _tabs.TabCount) % _tabs.TabCount;
    }

    private void ShowTabContextMenu(TabPage page, Point screenLocal)
    {
        var menu = new ContextMenuStrip();
        menu.Closed += (_, _) => menu.Dispose();
        menu.Items.Add("New Tab", null, (_, _) => { _ = OpenTabAsync(); });
        menu.Items.Add("Duplicate Tab", null, (_, _) =>
        {
            string url = GetTabLocation(page);
            _ = OpenTabAsync(url);
        });
        menu.Items.Add("Reopen Closed Tab", null, (_, _) => ReopenClosedTab())
            .Enabled = _recentlyClosed.Count > 0;
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Close Tab", null, (_, _) => CloseTab(page));
        menu.Items.Add("Close Other Tabs", null, (_, _) =>
        {
            foreach (var other in _tabs.TabPages.Cast<TabPage>().ToArray())
                if (other != page && _tabs.TabPages.Contains(other))
                    CloseTab(other);
        }).Enabled = _tabs.TabCount > 1;
        menu.Show(_tabs, screenLocal);
    }

}

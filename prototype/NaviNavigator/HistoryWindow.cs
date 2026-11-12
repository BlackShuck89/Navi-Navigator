using System.Drawing;
using System.Windows.Forms;

namespace NaviNavigator;

/// <summary>Independent browser-history manager (not Chromium's own history UI).</summary>
internal sealed class HistoryWindow : Form
{
    private readonly List<HistoryVisit> _visits;
    private readonly Action<string> _navigate;
    private readonly Action _persist;
    private readonly TextBox _search = new() { PlaceholderText = "Search titles or addresses…" };
    private readonly ListView _list = new()
    {
        View = View.Details,
        FullRowSelect = true,
        MultiSelect = true,
        HideSelection = false,
        Dock = DockStyle.Fill
    };
    private readonly Button _open = new() { Text = "Open in New Tab", AutoSize = true };
    private readonly Button _remove = new() { Text = "Remove Selected", AutoSize = true };
    private readonly Button _clear = new() { Text = "Clear All History", AutoSize = true };

    internal HistoryWindow(List<HistoryVisit> visits, Action<string> navigate, Action persist)
    {
        _visits = visits;
        _navigate = navigate;
        _persist = persist;

        Text = "Navi Navigator — History";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(900, 575);
        MinimumSize = new Size(620, 420);
        Icon = NaviArt.MakeIcon();

        var top = new Panel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(12, 10, 12, 8) };
        top.Controls.Add(_search);
        _search.Dock = DockStyle.Fill;
        _search.TextChanged += (_, _) => RefreshList();
        _list.Columns.Add("Visited", 165);
        _list.Columns.Add("Title", 280);
        _list.Columns.Add("Address", 420);
        _list.DoubleClick += (_, _) => OpenSelected();
        _list.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { OpenSelected(); e.Handled = true; }
            if (e.KeyCode == Keys.Delete) { RemoveSelected(); e.Handled = true; }
        };
        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(10, 7, 10, 5),
            FlowDirection = FlowDirection.LeftToRight
        };
        bottom.Controls.AddRange([_open, _remove, _clear]);
        _open.Click += (_, _) => OpenSelected();
        _remove.Click += (_, _) => RemoveSelected();
        _clear.Click += (_, _) =>
        {
            if (MessageBox.Show(this, "Delete all Navi Navigator browsing history?",
                    "Clear History", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                != DialogResult.Yes) return;
            _visits.Clear();
            _persist();
            RefreshList();
        };
        Controls.Add(_list);
        Controls.Add(top);
        Controls.Add(bottom);
        RefreshList();
    }

    private void RefreshList()
    {
        string query = _search.Text.Trim();
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var visit in _visits.Where(v =>
                         query.Length == 0 ||
                         v.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         v.Url.Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                var item = new ListViewItem(visit.VisitedUtc.LocalDateTime.ToString("g")) { Tag = visit };
                item.SubItems.Add(visit.Title);
                item.SubItems.Add(visit.Url);
                _list.Items.Add(item);
            }
        }
        finally { _list.EndUpdate(); }
    }

    private void OpenSelected()
    {
        if (_list.SelectedItems.Count != 1 ||
            _list.SelectedItems[0].Tag is not HistoryVisit visit) return;
        _navigate(visit.Url);
    }

    private void RemoveSelected()
    {
        var selected = _list.SelectedItems.Cast<ListViewItem>()
            .Select(x => x.Tag).OfType<HistoryVisit>().ToHashSet();
        if (selected.Count == 0) return;
        _visits.RemoveAll(selected.Contains);
        _persist();
        RefreshList();
    }
}

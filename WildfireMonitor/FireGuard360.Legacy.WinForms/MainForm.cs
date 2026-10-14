using System.Drawing;
using FireGuard360.Legacy.WinForms.Data;
using FireGuard360.Legacy.WinForms.Models;
using FireGuard360.Legacy.WinForms.Services;

namespace FireGuard360.Legacy.WinForms;

public sealed class MainForm : Form
{
    private readonly ComboBox _region = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _temperature = NumberBox(-60, 70, 32);
    private readonly NumericUpDown _humidity = NumberBox(0, 100, 25);
    private readonly NumericUpDown _wind = NumberBox(0, 200, 20);
    private readonly NumericUpDown _latitude = NumberBox(-90, 90, 62.45m, 4);
    private readonly NumericUpDown _longitude = NumberBox(-180, 180, -114.37m, 4);
    private readonly Label _riskScore = new() { AutoSize = true, Font = new Font("Segoe UI", 22, FontStyle.Bold) };
    private readonly Label _riskLevel = new() { AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
    private readonly TextBox _summary = MultiLineBox();
    private readonly TextBox _response = MultiLineBox();
    private readonly TextBox _log = MultiLineBox();
    private readonly DataGridView _history = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true };
    private readonly WildfireWorkflowService _workflow;
    private readonly LocalWildfireRepository _repository;
    private WorkflowResult? _latestResult;

    public MainForm()
    {
        _repository = new LocalWildfireRepository();
        _workflow = new WildfireWorkflowService(_repository);
        Text = "FireGuard360 Legacy Wildfire Management System";
        MinimumSize = new Size(1100, 720);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        _region.Items.AddRange(new object[] { "region-001", "region-002", "region-003" });
        _region.SelectedIndex = 0;
        Controls.Add(BuildLayout());
        RefreshHistory();
    }

    private Control BuildLayout()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildOperationsTab());
        tabs.TabPages.Add(BuildHistoryTab());

        var title = new Label
        {
            Text = "FIREGUARD360  |  LEGACY OPERATIONS CONSOLE",
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(18, 16, 0, 0),
            BackColor = Color.FromArgb(32, 47, 62),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        };

        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(tabs);
        panel.Controls.Add(title);
        return panel;
    }

    private TabPage BuildOperationsTab()
    {
        var page = new TabPage("Incident Operations");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(BuildInputPanel(), 0, 0);
        root.Controls.Add(BuildResultsPanel(), 1, 0);
        page.Controls.Add(root);
        return page;
    }

    private Control BuildInputPanel()
    {
        var group = new GroupBox { Text = "Sensor Reading", Dock = DockStyle.Fill, Padding = new Padding(14) };
        var form = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        AddField(form, "Region", _region);
        AddField(form, "Temperature (°C)", _temperature);
        AddField(form, "Humidity (%)", _humidity);
        AddField(form, "Wind speed (km/h)", _wind);
        AddField(form, "Latitude", _latitude);
        AddField(form, "Longitude", _longitude);

        var run = new Button { Text = "Run Wildfire Analysis", Height = 42, Dock = DockStyle.Top };
        run.Click += RunAnalysis;
        var export = new Button { Text = "Export Incident Report", Height = 36, Dock = DockStyle.Top };
        export.Click += ExportReport;

        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 92, FlowDirection = FlowDirection.TopDown };
        actions.Controls.Add(run);
        actions.Controls.Add(export);
        group.Controls.Add(actions);
        group.Controls.Add(form);
        return group;
    }

    private Control BuildResultsPanel()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };

        var overview = new TabPage("Risk & Response");
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, Padding = new Padding(14) };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        var riskPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        riskPanel.Controls.Add(_riskScore);
        riskPanel.Controls.Add(_riskLevel);
        content.Controls.Add(riskPanel, 0, 0);
        content.Controls.Add(new Label { Text = "Recommended Response", Dock = DockStyle.Fill, Font = BoldFont() }, 0, 1);
        content.Controls.Add(_response, 0, 2);
        content.Controls.Add(new Label { Text = "Executive Summary", Dock = DockStyle.Fill, Font = BoldFont() }, 0, 3);
        content.Controls.Add(_summary, 0, 4);
        overview.Controls.Add(content);

        var processing = new TabPage("Workflow Log");
        processing.Controls.Add(_log);
        tabs.TabPages.Add(overview);
        tabs.TabPages.Add(processing);
        return tabs;
    }

    private TabPage BuildHistoryTab()
    {
        var page = new TabPage("Incident History");
        page.Controls.Add(_history);
        return page;
    }

    private void RunAnalysis(object? sender, EventArgs e)
    {
        try
        {
            _log.Clear();
            var sensor = new SensorData
            {
                RegionId = _region.Text,
                Temperature = (double)_temperature.Value,
                Humidity = (double)_humidity.Value,
                WindSpeed = (double)_wind.Value,
                Location = new GeoLocation
                {
                    Latitude = (double)_latitude.Value,
                    Longitude = (double)_longitude.Value
                }
            };
            _latestResult = _workflow.Execute(sensor, WriteLog);
            _riskScore.Text = $"{_latestResult.Risk.RiskScore:F1}%";
            _riskLevel.Text = $"  {_latestResult.Risk.RiskLevel}";
            _riskLevel.ForeColor = RiskColor(_latestResult.Risk.RiskLevel);
            _response.Text = _latestResult.Response.Recommendations;
            _summary.Text = _latestResult.Summary.Summary;
            RefreshHistory();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Analysis failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            WriteLog($"ERROR: {exception.Message}");
        }
    }

    private void ExportReport(object? sender, EventArgs e)
    {
        if (_latestResult is null)
        {
            MessageBox.Show("Run an analysis before exporting a report.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Text report (*.txt)|*.txt",
            FileName = $"FireGuard360-{_latestResult.Sensor.RegionId}-{DateTime.Now:yyyyMMdd-HHmm}.txt"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        ReportService.ExportText(_latestResult, dialog.FileName);
        WriteLog($"Report exported to {dialog.FileName}");
    }

    private void RefreshHistory()
    {
        _history.DataSource = _repository.Load()
            .OrderByDescending(x => x.Sensor.Timestamp)
            .Select(x => new
            {
                x.Sensor.Timestamp,
                x.Sensor.RegionId,
                Temperature = x.Sensor.Temperature,
                Humidity = x.Sensor.Humidity,
                Wind = x.Sensor.WindSpeed,
                x.Risk.RiskScore,
                x.Risk.RiskLevel
            }).ToList();
    }

    private void WriteLog(string message) => _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");

    private static NumericUpDown NumberBox(decimal min, decimal max, decimal value, int decimals = 1) => new()
    {
        Minimum = min, Maximum = max, Value = value, DecimalPlaces = decimals, Dock = DockStyle.Fill
    };

    private static TextBox MultiLineBox() => new()
    {
        Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical,
        BackColor = Color.White
    };

    private static Font BoldFont() => new("Segoe UI", 10, FontStyle.Bold);

    private static void AddField(TableLayoutPanel form, string label, Control control)
    {
        var row = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        form.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        form.Controls.Add(control, 1, row);
    }

    private static Color RiskColor(string riskLevel) => riskLevel switch
    {
        "Extreme" => Color.DarkRed,
        "High" => Color.OrangeRed,
        "Moderate" => Color.DarkOrange,
        _ => Color.ForestGreen
    };
}

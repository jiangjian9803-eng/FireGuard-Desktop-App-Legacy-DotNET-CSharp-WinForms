using System.Drawing.Drawing2D;
using FireGuard360.Legacy.WinForms.Data;
using FireGuard360.Legacy.WinForms.Models;
using FireGuard360.Legacy.WinForms.Services;

namespace FireGuard360.Legacy.WinForms;

public sealed class MainForm : Form
{
    private static readonly Color Navy = Color.FromArgb(22, 36, 50);
    private static readonly Color Blue = Color.FromArgb(32, 99, 155);
    private static readonly Color Canvas = Color.FromArgb(239, 243, 247);
    private static readonly Color Muted = Color.FromArgb(101, 116, 130);

    private readonly Panel _contentHost = new() { Dock = DockStyle.Fill, BackColor = Canvas };
    private readonly Label _pageTitle = new() { AutoSize = true, Font = new Font("Segoe UI", 19, FontStyle.Bold), ForeColor = Navy };
    private readonly Label _pageSubtitle = new() { AutoSize = true, ForeColor = Muted };
    private readonly ToolStripStatusLabel _status = new("Ready");
    private readonly Dictionary<string, Control> _pages = new();
    private readonly List<Button> _navButtons = new();

    private readonly ComboBox _region = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _temperature = NumberBox(-60, 70, 32);
    private readonly NumericUpDown _humidity = NumberBox(0, 100, 25);
    private readonly NumericUpDown _wind = NumberBox(0, 200, 20);
    private readonly NumericUpDown _windDirection = NumberBox(0, 359, 225, 0);
    private readonly NumericUpDown _fuelDryness = NumberBox(0, 100, 65);
    private readonly NumericUpDown _latitude = NumberBox(-90, 90, 62.45m, 4);
    private readonly NumericUpDown _longitude = NumberBox(-180, 180, -114.37m, 4);

    private readonly Label _riskScore = ValueLabel("--");
    private readonly Label _riskLevel = ValueLabel("Not assessed", 13);
    private readonly Label _dashboardRiskScore = ValueLabel("--");
    private readonly Label _dashboardRiskLevel = ValueLabel("Not assessed", 13);
    private readonly Label _dashboardSpread = ValueLabel("--", 13);
    private readonly Label _dashboardPopulation = ValueLabel("--", 13);
    private readonly Label _dashboardResources = ValueLabel("--", 12);
    private readonly Label _incidentId = ValueLabel("No active assessment", 12);
    private readonly Label _spreadValue = ValueLabel("--", 13);
    private readonly Label _populationValue = ValueLabel("--", 13);
    private readonly Label _resourceValue = ValueLabel("--", 12);
    private readonly ProgressBar _riskBar = new() { Dock = DockStyle.Top, Height = 18, Maximum = 100 };
    private readonly TextBox _summary = ReadOnlyBox();
    private readonly TextBox _response = ReadOnlyBox();
    private readonly TextBox _log = ReadOnlyBox();
    private readonly DataGridView _history = Grid();
    private readonly DataGridView _audit = Grid();
    private readonly SituationMap _dashboardMap = new() { Dock = DockStyle.Fill };
    private readonly SituationMap _detailMap = new() { Dock = DockStyle.Fill };

    private readonly LocalWildfireRepository _repository;
    private readonly WildfireWorkflowService _workflow;
    private WorkflowResult? _latestResult;

    public MainForm()
    {
        _repository = new LocalWildfireRepository();
        _workflow = new WildfireWorkflowService(_repository);
        Text = "FireGuard360 | Wildfire Operations Management";
        MinimumSize = new Size(1240, 780);
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        BackColor = Canvas;

        _region.Items.AddRange(["region-001", "region-002", "region-003"]);
        _region.SelectedIndex = 0;

        Controls.Add(BuildShell());
        RegisterPages();
        RefreshAllData();
        ShowPage("Dashboard", "Operational overview and current wildfire posture");
    }

    private Control BuildShell()
    {
        var shell = new Panel { Dock = DockStyle.Fill };
        shell.Controls.Add(_contentHost);
        shell.Controls.Add(BuildHeader());
        shell.Controls.Add(BuildSidebar());
        shell.Controls.Add(new StatusStrip
        {
            BackColor = Color.White,
            Items = { _status, new ToolStripStatusLabel { Spring = true }, new ToolStripStatusLabel("LOCAL MODE  •  JSON DATA STORE") }
        });
        return shell;
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 82, BackColor = Color.White, Padding = new Padding(24, 14, 20, 8) };
        var text = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 720, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        text.Controls.Add(_pageTitle);
        text.Controls.Add(_pageSubtitle);
        header.Controls.Add(text);

        var mode = new Label
        {
            Text = "● SYSTEM ONLINE   |   DESKTOP OPERATIONS",
            AutoSize = true,
            ForeColor = Color.ForestGreen,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Dock = DockStyle.Right,
            Padding = new Padding(0, 17, 4, 0)
        };
        header.Controls.Add(mode);
        return header;
    }

    private Control BuildSidebar()
    {
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 236, BackColor = Navy, Padding = new Padding(0, 18, 0, 0) };
        var brand = new Label
        {
            Text = "  FIREGUARD360\n  LEGACY COMMAND",
            Dock = DockStyle.Top,
            Height = 76,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        sidebar.Controls.Add(brand);

        var navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(10, 16, 10, 0)
        };
        AddNav(navigation, "▣  Dashboard", "Dashboard", "Operational overview and current wildfire posture");
        AddNav(navigation, "⚠  Incident Analysis", "Analysis", "Environmental inputs and seven-stage wildfire assessment");
        AddNav(navigation, "⌖  Situation Map", "Map", "Projected fire corridor and regional assets");
        AddNav(navigation, "☷  Incident History", "History", "Stored assessments and audit records");
        AddNav(navigation, "▤  Resource Dispatch", "Resources", "Calculated crews, engines and aircraft requirements");
        AddNav(navigation, "≡  System Log", "Logs", "Workflow execution and operator audit trail");
        sidebar.Controls.Add(navigation);

        var footer = new Label
        {
            Text = "  v1.1  •  OFFLINE MODE",
            Dock = DockStyle.Bottom,
            Height = 42,
            ForeColor = Color.FromArgb(155, 170, 184),
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        sidebar.Controls.Add(footer);
        return sidebar;
    }

    private void AddNav(Control parent, string text, string page, string subtitle)
    {
        var button = new Button
        {
            Text = text,
            Width = 210,
            Height = 46,
            FlatStyle = FlatStyle.Flat,
            BackColor = Navy,
            ForeColor = Color.FromArgb(210, 220, 228),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) =>
        {
            foreach (var item in _navButtons) item.BackColor = Navy;
            button.BackColor = Blue;
            ShowPage(page, subtitle);
        };
        _navButtons.Add(button);
        parent.Controls.Add(button);
    }

    private void RegisterPages()
    {
        _pages["Dashboard"] = BuildDashboard();
        _pages["Analysis"] = BuildAnalysisPage();
        _pages["Map"] = BuildMapPage();
        _pages["History"] = BuildHistoryPage();
        _pages["Resources"] = BuildResourcesPage();
        _pages["Logs"] = BuildLogsPage();
        foreach (var page in _pages.Values)
        {
            page.Dock = DockStyle.Fill;
            page.Visible = false;
            _contentHost.Controls.Add(page);
        }
        if (_navButtons.Count > 0) _navButtons[0].BackColor = Blue;
    }

    private Control BuildDashboard()
    {
        var page = PagePanel();
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(0, 0, 0, 14) };
        for (var i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        cards.Controls.Add(Card("LATEST RISK", _dashboardRiskScore, "Calculated score"), 0, 0);
        cards.Controls.Add(Card("SEVERITY", _dashboardRiskLevel, "Operational classification"), 1, 0);
        cards.Controls.Add(Card("SPREAD FORECAST", _dashboardSpread, "Direction and distance"), 2, 0);
        cards.Controls.Add(Card("POPULATION AT RISK", _dashboardPopulation, "Rule-based estimate"), 3, 0);

        var lower = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        lower.Controls.Add(Section("Regional Situation Map", _dashboardMap), 0, 0);
        lower.Controls.Add(Section("Current Response Briefing", DashboardBriefing()), 1, 0);
        root.Controls.Add(cards, 0, 0);
        root.Controls.Add(lower, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private Control DashboardBriefing()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, Padding = new Padding(16) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.Controls.Add(new Label { Text = "ACTIVE ASSESSMENT", Dock = DockStyle.Fill, ForeColor = Muted, Font = BoldFont(9) }, 0, 0);
        layout.Controls.Add(_incidentId, 0, 1);
        layout.Controls.Add(_dashboardResources, 0, 2);
        layout.Controls.Add(new Label { Text = "EXECUTIVE SUMMARY", Dock = DockStyle.Fill, ForeColor = Muted, Font = BoldFont(9) }, 0, 3);
        layout.Controls.Add(_summary, 0, 4);
        var open = PrimaryButton("CREATE / UPDATE ASSESSMENT");
        open.Click += (_, _) => ShowPage("Analysis", "Environmental inputs and seven-stage wildfire assessment");
        layout.Controls.Add(open, 0, 5);
        return layout;
    }

    private Control BuildAnalysisPage()
    {
        var page = PagePanel();
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(Section("Environmental Observation", BuildInputForm()), 0, 0);
        root.Controls.Add(Section("Assessment Result", BuildAssessmentResult()), 1, 0);
        page.Controls.Add(root);
        return page;
    }

    private Control BuildInputForm()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(18) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 51));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 49));
        AddField(layout, "Operational region", _region);
        AddField(layout, "Temperature (°C)", _temperature);
        AddField(layout, "Relative humidity (%)", _humidity);
        AddField(layout, "Wind speed (km/h)", _wind);
        AddField(layout, "Wind direction (°)", _windDirection);
        AddField(layout, "Fuel dryness (%)", _fuelDryness);
        AddField(layout, "Latitude", _latitude);
        AddField(layout, "Longitude", _longitude);

        var note = new Label
        {
            Text = "Decision-support prototype. Scores are generated from configurable demonstration rules and require operator review.",
            Dock = DockStyle.Fill,
            ForeColor = Muted,
            AutoSize = true,
            Padding = new Padding(0, 12, 0, 8)
        };
        var noteRow = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        layout.Controls.Add(note, 0, noteRow);
        layout.SetColumnSpan(note, 2);

        var run = PrimaryButton("RUN SEVEN-STAGE ASSESSMENT");
        run.Height = 48;
        run.Click += RunAnalysis;
        var runRow = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.Controls.Add(run, 0, runRow);
        layout.SetColumnSpan(run, 2);
        return layout;
    }

    private Control BuildAssessmentResult()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 8, Padding = new Padding(18) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        var riskLine = new FlowLayoutPanel { Dock = DockStyle.Fill };
        riskLine.Controls.Add(_riskScore);
        riskLine.Controls.Add(_riskLevel);
        layout.Controls.Add(riskLine, 0, 0);
        layout.Controls.Add(_riskBar, 0, 1);
        layout.Controls.Add(LabeledValue("Projected spread", _spreadValue), 0, 2);
        layout.Controls.Add(LabeledValue("Population exposure", _populationValue), 0, 3);
        layout.Controls.Add(LabeledValue("Resource requirement", _resourceValue), 0, 4);
        layout.Controls.Add(new Label { Text = "RESPONSE RECOMMENDATION", Dock = DockStyle.Fill, Font = BoldFont(9), ForeColor = Muted }, 0, 5);
        layout.Controls.Add(_response, 0, 6);
        var export = SecondaryButton("EXPORT INCIDENT REPORT");
        export.Click += ExportReport;
        layout.Controls.Add(export, 0, 7);
        return layout;
    }

    private Control BuildMapPage()
    {
        var page = PagePanel();
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        root.Controls.Add(Section("Projected Fire Corridor", _detailMap), 0, 0);

        var legend = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), RowCount = 7 };
        legend.Controls.Add(Heading("Map Legend"), 0, 0);
        legend.Controls.Add(LegendItem(Color.OrangeRed, "Sensor / ignition point"), 0, 1);
        legend.Controls.Add(LegendItem(Color.Gold, "Projected spread corridor"), 0, 2);
        legend.Controls.Add(LegendItem(Color.SteelBlue, "Critical infrastructure"), 0, 3);
        legend.Controls.Add(Heading("Assessment Notes"), 0, 4);
        var notes = new Label { Text = "Run an incident assessment to update the projected direction, distance, and affected assets.", Dock = DockStyle.Fill, ForeColor = Muted };
        legend.Controls.Add(notes, 0, 5);
        legend.Controls.Add(new Label { Text = "Map is a schematic operational view, not a GIS navigation product.", Dock = DockStyle.Fill, ForeColor = Color.DarkRed }, 0, 6);
        root.Controls.Add(Section("Operational Context", legend), 1, 0);
        page.Controls.Add(root);
        return page;
    }

    private Control BuildHistoryPage()
    {
        var page = PagePanel();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        layout.Controls.Add(Section("Stored Incident Assessments", _history), 0, 0);
        layout.Controls.Add(Section("Selected Workflow Audit Trail", _audit), 0, 1);
        _history.SelectionChanged += (_, _) => RefreshSelectedAudit();
        page.Controls.Add(layout);
        return page;
    }

    private Control BuildResourcesPage()
    {
        var page = PagePanel();
        var grid = Grid();
        grid.DataSource = new[]
        {
            new { Resource = "Wildland crews", Available = 12, Assigned = 0, Status = "Ready" },
            new { Resource = "Type 3 engines", Available = 8, Assigned = 0, Status = "Ready" },
            new { Resource = "Air tankers", Available = 3, Assigned = 0, Status = "Weather dependent" },
            new { Resource = "Helicopters", Available = 4, Assigned = 0, Status = "Ready" },
            new { Resource = "Evacuation buses", Available = 6, Assigned = 0, Status = "Regional standby" }
        };
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        split.Controls.Add(Section("Resource Availability", grid), 0, 0);
        var decision = ReadOnlyBox();
        decision.Text = "Resource requirements are calculated from the assessed risk level. Dispatch remains an operator decision; the application does not automatically deploy personnel or equipment.";
        split.Controls.Add(Section("Dispatch Governance", decision), 1, 0);
        page.Controls.Add(split);
        return page;
    }

    private Control BuildLogsPage()
    {
        var page = PagePanel();
        page.Controls.Add(Section("Application and Workflow Log", _log));
        return page;
    }

    private void RunAnalysis(object? sender, EventArgs e)
    {
        try
        {
            _log.Clear();
            _status.Text = "Running wildfire assessment...";
            var sensor = new SensorData
            {
                RegionId = _region.Text,
                Temperature = (double)_temperature.Value,
                Humidity = (double)_humidity.Value,
                WindSpeed = (double)_wind.Value,
                WindDirection = (int)_windDirection.Value,
                FuelDryness = (double)_fuelDryness.Value,
                Location = new GeoLocation { Latitude = (double)_latitude.Value, Longitude = (double)_longitude.Value }
            };

            _latestResult = _workflow.Execute(sensor, WriteLog);
            UpdateCurrentAssessment(_latestResult);
            RefreshAllData();
            _status.Text = $"Assessment {_latestResult.IncidentId} completed and saved";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Assessment failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            WriteLog($"ERROR: {exception.Message}");
            _status.Text = "Assessment failed";
        }
    }

    private void UpdateCurrentAssessment(WorkflowResult result)
    {
        _incidentId.Text = $"{result.IncidentId}  •  {result.Sensor.RegionId}";
        _riskScore.Text = $"{result.Risk.RiskScore:F1}%";
        _riskLevel.Text = result.Risk.RiskLevel.ToUpperInvariant();
        _riskLevel.ForeColor = RiskColor(result.Risk.RiskLevel);
        _dashboardRiskScore.Text = _riskScore.Text;
        _dashboardRiskLevel.Text = _riskLevel.Text;
        _dashboardRiskLevel.ForeColor = _riskLevel.ForeColor;
        _riskBar.Value = Math.Clamp((int)Math.Round(result.Risk.RiskScore), 0, 100);
        _spreadValue.Text = $"{result.Spread.Direction} / {result.Spread.EstimatedDistanceKm:F1} km";
        _populationValue.Text = $"{result.Impact.PopulationAtRisk:N0} people";
        _resourceValue.Text = $"{result.Response.RequiredCrews} crews  •  {result.Response.RequiredEngines} engines  •  {result.Response.RequiredAircraft} aircraft";
        _dashboardSpread.Text = _spreadValue.Text;
        _dashboardPopulation.Text = _populationValue.Text;
        _dashboardResources.Text = _resourceValue.Text;
        _response.Text = $"{result.Response.Priority}\r\n\r\n{result.Response.Recommendations}\r\n\r\nRisk basis: {result.Risk.Explanation}";
        _summary.Text = result.Summary.Summary;
        _dashboardMap.UpdateAssessment(result);
        _detailMap.UpdateAssessment(result);
    }

    private void ExportReport(object? sender, EventArgs e)
    {
        if (_latestResult is null)
        {
            MessageBox.Show("Run an assessment before exporting a report.", "No assessment", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Text report (*.txt)|*.txt",
            FileName = $"{_latestResult.IncidentId}-report.txt"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        ReportService.ExportText(_latestResult, dialog.FileName);
        WriteLog($"Report exported to {dialog.FileName}");
        _status.Text = "Incident report exported";
    }

    private void RefreshAllData()
    {
        var records = _repository.Load().OrderByDescending(x => x.Sensor.Timestamp).ToList();
        _history.DataSource = records.Select(x => new
        {
            x.IncidentId,
            LocalTime = x.Sensor.Timestamp.ToLocalTime(),
            Region = x.Sensor.RegionId,
            Risk = x.Risk.RiskScore,
            Severity = x.Risk.RiskLevel,
            x.Status,
            Population = x.Impact.PopulationAtRisk,
            Priority = x.Response.Priority
        }).ToList();
        _history.Tag = records;

        if (_latestResult is null && records.Count > 0)
        {
            _latestResult = records[0];
            UpdateCurrentAssessment(_latestResult);
        }
        RefreshSelectedAudit();
    }

    private void RefreshSelectedAudit()
    {
        if (_history.Tag is not List<WorkflowResult> records || records.Count == 0)
        {
            _audit.DataSource = null;
            return;
        }
        var index = _history.CurrentRow?.Index ?? 0;
        if (index < 0 || index >= records.Count) index = 0;
        var auditTrail = records[index].AuditTrail ?? Array.Empty<AuditEvent>();
        _audit.DataSource = auditTrail.Select(x => new { Time = x.Timestamp.ToLocalTime(), x.Stage, x.Detail }).ToList();
    }

    private void ShowPage(string name, string subtitle)
    {
        foreach (var page in _pages.Values) page.Visible = false;
        if (_pages.TryGetValue(name, out var selected))
        {
            selected.Visible = true;
            selected.BringToFront();
        }
        _pageTitle.Text = name;
        _pageSubtitle.Text = subtitle;
    }

    private void WriteLog(string message) => _log.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");

    private static Panel PagePanel() => new() { Dock = DockStyle.Fill, Padding = new Padding(22), BackColor = Canvas };

    private static Control Section(string title, Control content)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
        var header = new Label
        {
            Text = "  " + title,
            Dock = DockStyle.Top,
            Height = 43,
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Navy,
            Font = BoldFont(11),
            TextAlign = ContentAlignment.MiddleLeft
        };
        content.Dock = DockStyle.Fill;
        panel.Controls.Add(content);
        panel.Controls.Add(header);
        return panel;
    }

    private static Control Card(string title, Label value, string hint)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 14, 0), Padding = new Padding(17, 13, 12, 9) };
        var heading = new Label { Text = title, Dock = DockStyle.Top, Height = 22, ForeColor = Muted, Font = BoldFont(8) };
        var caption = new Label { Text = hint, Dock = DockStyle.Bottom, Height = 20, ForeColor = Muted, Font = new Font("Segoe UI", 8) };
        value.Dock = DockStyle.Fill;
        panel.Controls.Add(value);
        panel.Controls.Add(caption);
        panel.Controls.Add(heading);
        return panel;
    }

    private static Control LabeledValue(string caption, Label value)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        panel.Controls.Add(new Label { Text = caption + ":", AutoSize = true, ForeColor = Muted, Font = BoldFont(9), Padding = new Padding(0, 4, 8, 0) });
        panel.Controls.Add(value);
        return panel;
    }

    private static Label Heading(string text) => new() { Text = text.ToUpperInvariant(), Dock = DockStyle.Fill, Font = BoldFont(10), ForeColor = Navy };

    private static Control LegendItem(Color color, string text)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        panel.Controls.Add(new Label { Text = "●", ForeColor = color, AutoSize = true, Font = BoldFont(14) });
        panel.Controls.Add(new Label { Text = text, ForeColor = Navy, AutoSize = true, Padding = new Padding(5, 5, 0, 0) });
        return panel;
    }

    private static void AddField(TableLayoutPanel layout, string label, Control control)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Navy }, 0, row);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(3, 8, 3, 8);
        layout.Controls.Add(control, 1, row);
    }

    private static Button PrimaryButton(string text)
    {
        var button = new Button { Text = text, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Blue, ForeColor = Color.White, Font = BoldFont(9), Cursor = Cursors.Hand };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static Button SecondaryButton(string text)
    {
        var button = PrimaryButton(text);
        button.BackColor = Color.White;
        button.ForeColor = Blue;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Blue;
        return button;
    }

    private static NumericUpDown NumberBox(decimal min, decimal max, decimal value, int decimals = 1) => new()
    {
        Minimum = min, Maximum = max, Value = value, DecimalPlaces = decimals, ThousandsSeparator = true
    };

    private static TextBox ReadOnlyBox() => new()
    {
        Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Vertical,
        BackColor = Color.White, ForeColor = Navy, Dock = DockStyle.Fill
    };

    private static Label ValueLabel(string text, float size = 20) => new()
    {
        Text = text, AutoSize = true, Font = new Font("Segoe UI", size, FontStyle.Bold), ForeColor = Navy,
        Padding = new Padding(0, 2, 8, 0)
    };

    private static Font BoldFont(float size) => new("Segoe UI", size, FontStyle.Bold);

    private static DataGridView Grid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = Color.White,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false
        };
        grid.ColumnHeadersDefaultCellStyle.BackColor = Navy;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = BoldFont(9);
        grid.ColumnHeadersHeight = 36;
        grid.RowTemplate.Height = 32;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 251);
        return grid;
    }

    private static Color RiskColor(string level) => level switch
    {
        "Extreme" => Color.DarkRed,
        "High" => Color.OrangeRed,
        "Moderate" => Color.DarkOrange,
        _ => Color.ForestGreen
    };
}

internal sealed class SituationMap : Panel
{
    private WorkflowResult? _assessment;

    public SituationMap()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(224, 232, 222);
    }

    public void UpdateAssessment(WorkflowResult assessment)
    {
        _assessment = assessment;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var gridPen = new Pen(Color.FromArgb(190, 204, 190), 1);
        for (var x = 0; x < Width; x += 55) graphics.DrawLine(gridPen, x, 0, x, Height);
        for (var y = 0; y < Height; y += 55) graphics.DrawLine(gridPen, 0, y, Width, y);

        using var roadPen = new Pen(Color.White, 9);
        graphics.DrawBezier(roadPen, 0, Height * .72f, Width * .30f, Height * .45f, Width * .62f, Height * .8f, Width, Height * .34f);
        using var roadEdge = new Pen(Color.FromArgb(177, 184, 174), 1);
        graphics.DrawBezier(roadEdge, 0, Height * .72f, Width * .30f, Height * .45f, Width * .62f, Height * .8f, Width, Height * .34f);

        DrawAsset(graphics, new PointF(Width * .24f, Height * .30f), "HOSPITAL");
        DrawAsset(graphics, new PointF(Width * .72f, Height * .68f), "POWER / WATER");

        var origin = new PointF(Width * .43f, Height * .56f);
        var angle = ((_assessment?.Sensor.WindDirection ?? 45) - 90) * Math.PI / 180;
        var length = Math.Min(Width, Height) * .30f;
        var end = new PointF(origin.X + (float)Math.Cos(angle) * length, origin.Y + (float)Math.Sin(angle) * length);
        using var corridor = new Pen(Color.FromArgb(175, 255, 183, 50), 26) { EndCap = LineCap.ArrowAnchor };
        graphics.DrawLine(corridor, origin, end);
        graphics.FillEllipse(Brushes.OrangeRed, origin.X - 11, origin.Y - 11, 22, 22);
        graphics.DrawString("SENSOR / INCIDENT", new Font("Segoe UI", 8, FontStyle.Bold), Brushes.DarkRed, origin.X + 14, origin.Y - 10);

        var title = _assessment is null
            ? "NO ACTIVE ASSESSMENT — DISPLAYING DEFAULT REGION"
            : $"{_assessment.IncidentId}  |  {_assessment.Risk.RiskLevel.ToUpperInvariant()} RISK  |  {_assessment.Spread.Direction} {_assessment.Spread.EstimatedDistanceKm:F1} KM";
        graphics.FillRectangle(new SolidBrush(Color.FromArgb(215, 22, 36, 50)), 12, 12, Math.Max(Width - 24, 100), 36);
        graphics.DrawString(title, new Font("Segoe UI", 10, FontStyle.Bold), Brushes.White, 25, 21);
    }

    private static void DrawAsset(Graphics graphics, PointF point, string label)
    {
        graphics.FillRectangle(Brushes.SteelBlue, point.X - 8, point.Y - 8, 16, 16);
        graphics.DrawString(label, new Font("Segoe UI", 7, FontStyle.Bold), Brushes.DarkSlateGray, point.X + 12, point.Y - 7);
    }
}

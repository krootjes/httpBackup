using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using httpBackupCore;

namespace httpBackupTray;

public sealed class SettingsForm : Form
{
    private readonly NumericUpDown nudInterval = new();
    private readonly TextBox txtFolder = new();
    private readonly Button btnBrowse = new();
    private readonly DataGridView dgvSites = new();
    private readonly Button btnAdd = new();
    private readonly Button btnEdit = new();
    private readonly Button btnRemove = new();
    private readonly Button btnSave = new();
    private readonly Button btnCancel = new();

    private AppConfig _config;
    private BindingList<BackupSite> _sitesBinding;

    public SettingsForm()
    {
        Text = "HttpBackup Settings";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 900;
        Height = 600;
        MinimumSize = new System.Drawing.Size(780, 520);

        _config = ConfigStore.LoadOrCreateDefault();
        _sitesBinding = new BindingList<BackupSite>(_config.Sites);

        BuildLayout();
        LoadValuesIntoUI();
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // interval + folder
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grid
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // site buttons
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // save/cancel

        Controls.Add(layout);

        // Top panel (interval + folder)
        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 2,
            AutoSize = true
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Interval
        var lblInterval = new Label { Text = "Global interval (minutes):", AutoSize = true, Anchor = AnchorStyles.Left };
        nudInterval.Minimum = 1;
        nudInterval.Maximum = 10080;
        nudInterval.Width = 120;
        nudInterval.Anchor = AnchorStyles.Left;

        // Folder
        var lblFolder = new Label { Text = "Backup folder:", AutoSize = true, Anchor = AnchorStyles.Left };
        txtFolder.Dock = DockStyle.Fill;

        btnBrowse.Text = "Browse…";
        btnBrowse.AutoSize = true;
        btnBrowse.Click += (_, _) => BrowseFolder();

        top.Controls.Add(lblInterval, 0, 0);
        top.Controls.Add(nudInterval, 1, 0);
        top.SetColumnSpan(nudInterval, 2);

        top.Controls.Add(lblFolder, 0, 1);
        top.Controls.Add(txtFolder, 1, 1);
        top.Controls.Add(btnBrowse, 2, 1);

        layout.Controls.Add(top, 0, 0);

        // Grid
        dgvSites.Dock = DockStyle.Fill;
        dgvSites.AutoGenerateColumns = false;
        dgvSites.AllowUserToAddRows = false;
        dgvSites.AllowUserToDeleteRows = false;
        dgvSites.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvSites.MultiSelect = false;
        dgvSites.ReadOnly = true;

        dgvSites.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(BackupSite.Enabled),
            HeaderText = "Enabled",
            Width = 70
        });

        dgvSites.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(BackupSite.Name),
            HeaderText = "Name (prefix)",
            Width = 180
        });

        dgvSites.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(BackupSite.Url),
            HeaderText = "URL",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        dgvSites.DataSource = _sitesBinding;

        layout.Controls.Add(dgvSites, 0, 1);

        // Site buttons row
        var siteButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false
        };

        btnAdd.Text = "Add…";
        btnEdit.Text = "Edit…";
        btnRemove.Text = "Remove";

        btnAdd.Click += (_, _) => AddSite();
        btnEdit.Click += (_, _) => EditSelectedSite();
        btnRemove.Click += (_, _) => RemoveSelectedSite();

        siteButtons.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnRemove });
        layout.Controls.Add(siteButtons, 0, 2);

        // Save/Cancel row
        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false
        };

        btnSave.Text = "Save";
        btnCancel.Text = "Cancel";

        btnSave.Click += (_, _) => SaveConfig();
        btnCancel.Click += (_, _) => Close();

        bottom.Controls.AddRange(new Control[] { btnSave, btnCancel });
        layout.Controls.Add(bottom, 0, 3);
    }

    private void LoadValuesIntoUI()
    {
        nudInterval.Value = Math.Clamp(_config.IntervalMinutes, (int)nudInterval.Minimum, (int)nudInterval.Maximum);
        txtFolder.Text = _config.BackupFolder ?? "";
    }

    private void BrowseFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select backup folder",
            UseDescriptionForTitle = true
        };

        if (Directory.Exists(txtFolder.Text))
            dlg.SelectedPath = txtFolder.Text;

        if (dlg.ShowDialog(this) == DialogResult.OK)
            txtFolder.Text = dlg.SelectedPath;
    }

    private BackupSite? GetSelectedSite()
    {
        if (dgvSites.SelectedRows.Count == 0) return null;
        return dgvSites.SelectedRows[0].DataBoundItem as BackupSite;
    }

    private void AddSite()
    {
        var site = new BackupSite { Enabled = true, Name = "site", Url = "https://example.com" };
        using var dlg = new SiteEditDialog(site, isNew: true);

        if (dlg.ShowDialog(this) == DialogResult.OK)
            _sitesBinding.Add(dlg.Site);
    }

    private void EditSelectedSite()
    {
        var selected = GetSelectedSite();
        if (selected is null) return;

        // Clone to avoid editing until OK
        var clone = new BackupSite
        {
            Enabled = selected.Enabled,
            Name = selected.Name,
            Url = selected.Url
        };

        using var dlg = new SiteEditDialog(clone, isNew: false);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        selected.Enabled = dlg.Site.Enabled;
        selected.Name = dlg.Site.Name;
        selected.Url = dlg.Site.Url;

        dgvSites.Refresh();
    }

    private void RemoveSelectedSite()
    {
        var selected = GetSelectedSite();
        if (selected is null) return;

        var res = MessageBox.Show($"Remove '{selected.Name}'?", "HttpBackup", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (res == DialogResult.Yes)
            _sitesBinding.Remove(selected);
    }

    private void SaveConfig()
    {
        var interval = (int)nudInterval.Value;
        var folder = (txtFolder.Text ?? "").Trim();

        if (interval < 1)
        {
            MessageBox.Show("Interval must be at least 1 minute.", "HttpBackup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(folder))
        {
            MessageBox.Show("Backup folder is required.", "HttpBackup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Basic validation for sites
        foreach (var s in _sitesBinding)
        {
            if (string.IsNullOrWhiteSpace(s.Name))
            {
                MessageBox.Show("Each site must have a Name (prefix).", "HttpBackup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Uri.TryCreate(s.Url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                MessageBox.Show($"Invalid URL for site '{s.Name}': {s.Url}", "HttpBackup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (HasInvalidFileNameChars(s.Name))
            {
                MessageBox.Show($"Site name contains invalid filename characters: {s.Name}", "HttpBackup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        _config.IntervalMinutes = interval;
        _config.BackupFolder = folder;
        _config.Sites = _sitesBinding.ToList();

        try
        {
            ConfigStore.Save(_config);
            MessageBox.Show("Saved!", "HttpBackup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed:\n{ex.Message}", "HttpBackup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool HasInvalidFileNameChars(string name)
        => name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
}

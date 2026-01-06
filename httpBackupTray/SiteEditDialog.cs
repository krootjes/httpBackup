using System;
using System.Windows.Forms;
using httpBackupCore;

namespace httpBackupTray;

public sealed class SiteEditDialog : Form
{
    private readonly CheckBox chkEnabled = new() { Text = "Enabled", AutoSize = true };
    private readonly TextBox txtName = new();
    private readonly TextBox txtUrl = new();
    private readonly Button btnOk = new() { Text = "OK" };
    private readonly Button btnCancel = new() { Text = "Cancel" };

    public BackupSite Site { get; }

    public SiteEditDialog(BackupSite site, bool isNew)
    {
        Site = site;

        Text = isNew ? "Add website" : "Edit website";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 560;
        Height = 230;

        BuildLayout();
        LoadSite();
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 4
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Controls.Add(layout);

        layout.Controls.Add(chkEnabled, 1, 0);

        layout.Controls.Add(new Label { Text = "Name (prefix):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        txtName.Dock = DockStyle.Fill;
        layout.Controls.Add(txtName, 1, 1);

        layout.Controls.Add(new Label { Text = "URL:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        txtUrl.Dock = DockStyle.Fill;
        layout.Controls.Add(txtUrl, 1, 2);

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false
        };

        btnOk.Click += (_, _) => OnOk();
        btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        bottom.Controls.Add(btnOk);
        bottom.Controls.Add(btnCancel);

        layout.Controls.Add(bottom, 1, 3);
    }

    private void LoadSite()
    {
        chkEnabled.Checked = Site.Enabled;
        txtName.Text = Site.Name ?? "";
        txtUrl.Text = Site.Url ?? "";
    }

    private void OnOk()
    {
        var name = (txtName.Text ?? "").Trim();
        var url = (txtUrl.Text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Name is required.", "HttpBackup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            MessageBox.Show("URL must be a valid http/https URL.", "HttpBackup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Site.Enabled = chkEnabled.Checked;
        Site.Name = name;
        Site.Url = url;

        DialogResult = DialogResult.OK;
    }
}

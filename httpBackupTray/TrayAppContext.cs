using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using httpBackupCore;

namespace httpBackupTray;

public sealed class TrayAppContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private SettingsForm? _settingsForm;

    public TrayAppContext()
    {
        var menu = new ContextMenuStrip();

        var mnuOpen = new ToolStripMenuItem("Open settings…", null, (_, _) => ShowSettings());
        var mnuRunNow = new ToolStripMenuItem("Run backup now", null, (_, _) => RunNow());
        var mnuOpenFolder = new ToolStripMenuItem("Open backup folder", null, (_, _) => OpenBackupFolder());
        var mnuExit = new ToolStripMenuItem("Exit", null, (_, _) => Exit());

        menu.Items.Add(mnuOpen);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(mnuRunNow);
        menu.Items.Add(mnuOpenFolder);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(mnuExit);

        _notifyIcon = new NotifyIcon
        {
            Text = "HttpBackup",
            ContextMenuStrip = menu,
            Visible = true,
            Icon = SystemIcons.Application
        };

        _notifyIcon.DoubleClick += (_, _) => ShowSettings();
    }

    private void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.BringToFront();
            _settingsForm.Activate();
            return;
        }

        _settingsForm = new SettingsForm();
        _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        _settingsForm.Show();
        _settingsForm.BringToFront();
        _settingsForm.Activate();
    }

    private void RunNow()
    {
        // Placeholder: later laat je dit de worker/service triggeren
        _notifyIcon.ShowBalloonTip(
            timeout: 1500,
            tipTitle: "HttpBackup",
            tipText: "Run now is not implemented yet (only tray/UI done).",
            tipIcon: ToolTipIcon.Info);
    }

    private void OpenBackupFolder()
    {
        var cfg = ConfigStore.LoadOrCreateDefault();
        try
        {
            Directory.CreateDirectory(cfg.BackupFolder);
            Process.Start(new ProcessStartInfo
            {
                FileName = cfg.BackupFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open folder:\n{ex.Message}", "HttpBackup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Exit()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _settingsForm?.Close();
        ExitThread();
    }
}
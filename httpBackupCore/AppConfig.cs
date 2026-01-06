using System;
using System.Collections.Generic;

namespace httpBackupCore;

public sealed class AppConfig
{
    public int IntervalMinutes { get; set; } = 60;
    public string BackupFolder { get; set; } = @"C:\Backups\HttpBackup";
    public List<BackupSite> Sites { get; set; } = new();
}

public sealed class BackupSite
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "site";
    public string Url { get; set; } = "https://example.com";
}

﻿namespace PlexWatch.Utilities;

public class Configuration
{
    public string PlexToken { get; set; } = null!;
    public string BindAddress { get; set; } = null!;
    public bool Debug { get; set; }
}

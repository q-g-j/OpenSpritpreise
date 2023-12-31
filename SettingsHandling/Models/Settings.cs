﻿namespace SettingsHandling.Models;

public class Settings
{
    public string? TankerkoenigApiKey { get; set; }
    public string? LastKnownStreet { get; set; }
    public string? LastKnownPostalCode { get; set; }
    public string? LastKnownCity { get; set; }
    public double? LastKnownLatitude { get; set; }
    public double? LastKnownLongitude { get; set; }
    public string? LastKnownRadius { get; set; } = "5";
    public string? GasType { get; set; } = "E5";
    public string? SortBy { get; set; } = "Price";
}
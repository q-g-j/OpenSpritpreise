﻿using Avalonia;
using Avalonia.Controls.Primitives;
using OpenSpritpreise.Models;

namespace OpenSpritpreise.CustomControls;

public class StationCardControl : TemplatedControl
{
    public static readonly StyledProperty<DisplayStation> StationProperty =
        AvaloniaProperty.Register<StationItemControl, DisplayStation>(nameof(Station));

    public DisplayStation Station
    {
        get => GetValue(StationProperty);
        set => SetValue(StationProperty, value);
    }
}
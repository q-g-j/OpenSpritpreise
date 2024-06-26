﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using OpenSpritpreise.Extensions;
using OpenSpritpreise.Services;
using OpenSpritpreise.ViewModels;
using OpenSpritpreise.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using OpenSpritpreise.PageTransitions;
using OpenSpritpreise.Store;
using Serilog;

namespace OpenSpritpreise;

public class App : Application
{
    private readonly IHost? _host = new HostBuilder()
        .AddServices()
        .Build();

    public event Action? BackPressed;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _host?.Start();
        
        Log.Logger = new LoggerConfiguration()
            .WriteTo.BrowserConsole()
            .WriteTo.Debug()
            .CreateLogger();

        var viewLocator = _host?.Services.GetService<ViewLocatorService>();
        DataTemplates.Add(viewLocator!);

        var navigationService = _host?.Services.GetRequiredService<NavigationService<MainNavigationStore>>();
        navigationService?.Navigate<AddressSelectionViewModel, CustomCrossFadePageTransition>();

        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow
                {
                    DataContext = _host?.Services.GetService<MainViewModel>()
                };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = _host?.Services.GetService<MainViewModel>()
                };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void OnBackPressed()
    {
        BackPressed?.Invoke();
    }

    public bool IsBackPressedSubscribed()
    {
        return BackPressed != null;
    }

    public static App GetCurrent()
    {
        return (App)Current!;
    }
}
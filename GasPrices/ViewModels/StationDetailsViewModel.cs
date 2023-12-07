using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BrowserInterop;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GasPrices.Models;
using GasPrices.Store;
using Xamarin.Essentials;

namespace GasPrices.ViewModels;

public partial class StationDetailsViewModel : ViewModelBase
{
    #region constructors

    public StationDetailsViewModel()
    {
    }

    public StationDetailsViewModel(AppStateStore appStateStore)
    {
        Station = appStateStore.SelectedStation;
    }

    #endregion constructors

    #region bindable properties

    [ObservableProperty] private DisplayStation? _station;

    #endregion bindable properties

    #region commands

    [RelayCommand]
    public async Task OpenInBrowser()
    {
        var url = $"https://www.google.com/maps/search/{Station!.GetUriData()}";
        await OpenBrowser(new Uri(url));
    }

    #endregion commands

    #region private methods

    private static async Task OpenBrowser(Uri uri)
    {
        if (OperatingSystem.IsBrowser())
        {
            JsUrlHandler.OpenInNewTab(uri.ToString());
        }
        else if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            await Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
        }
        else
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
        }
    }

    #endregion private methods

    #region public overrides

    public override void Dispose()
    {
    }

    #endregion public overrides
}
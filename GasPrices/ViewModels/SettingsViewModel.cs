using ApiClients;
using ApiClients.Models;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GasPrices.Services;
using HttpClient.Exceptions;
using SettingsHandling.Models;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GasPrices.PageTransitions;
using GasPrices.Store;
using SettingsHandling;

namespace GasPrices.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    #region constructors

    public SettingsViewModel()
    {
    }

    public SettingsViewModel(
        NavigationService<MainNavigationStore> mainNavigationService,
        ISettingsReader? settingsReader,
        ISettingsWriter? settingsWriter,
        IGasPricesClient gasPricesClient)
    {
        _mainNavigationService = mainNavigationService;
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;
        _gasPricesClient = gasPricesClient;

        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            CancelButtonIsVisible = false;
        }

        App.GetCurrent().BackPressed += OnBackPressed;
    }

    #endregion constructors

    #region private fields

    private readonly NavigationService<MainNavigationStore>? _mainNavigationService;
    private readonly ISettingsReader? _settingsReader;
    private readonly ISettingsWriter? _settingsWriter;
    private readonly IGasPricesClient? _gasPricesClient;

    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isValidated;

    #endregion private fields

    #region bindable properties

    [ObservableProperty] private string _tankerKoenigApiKey = string.Empty;
    [ObservableProperty] private bool _validateButtonIsEnabled;
    [ObservableProperty] private bool _saveButtonIsEnabled;
    [ObservableProperty] private string _noticeTitleText = string.Empty;
    [ObservableProperty] private string _noticeText = string.Empty;
    [ObservableProperty] private IBrush _noticeTextColor = new SolidColorBrush(Color.Parse("Orange"));
    [ObservableProperty] private bool _noticeTextIsVisible;
    [ObservableProperty] private bool _progressRingIsActive;
    [ObservableProperty] private bool _cancelButtonIsVisible = true;

    #endregion bindable properties

    #region commands

    [RelayCommand]
    private async Task Initialized()
    {
        var settings = await _settingsReader!.ReadAsync();
        if (settings != null && !string.IsNullOrEmpty(settings.TankerkoenigApiKey))
        {
            TankerKoenigApiKey = settings.TankerkoenigApiKey;
        }
    }

    [RelayCommand]
    private Task KeyDown(object sender)
    {
        var e = sender as KeyEventArgs;
        if (e?.Key != Key.Enter && e?.Key != Key.Return) return Task.CompletedTask;
        e.Handled = true;
        if (_isValidated)
        {
            return Save();
        }
        else if (ValidateButtonIsEnabled)
        {
            return Validate();
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Validate()
    {
        var coords = new Coords(11.601314, 48.135788);

        ProgressRingIsActive = true;

        try
        {
            var stations = await _gasPricesClient!.GetStationsAsync(TankerKoenigApiKey, coords, 1);

            if (stations == null)
            {
                ShowWarning(true, "Der API-Schlüssel wurde nicht angenommen!", 2000);
            }
            else
            {
                _isValidated = true;
                SaveButtonIsEnabled = true;
                ShowWarning(false, "Der API-Schlüssel wurde angenommen!", 2000);
            }
        }
        catch (HttpClientException ex)
        {
            var message = new StringBuilder();
            message.AppendLine("Keine Verbindung zu Tankerkönig.de!\n");
            message.AppendLine("Fehlermeldung:");
            message.Append(ex.Message);
            ShowWarning(true, message.ToString(), 5000);
        }
        catch (BadStatuscodeException ex)
        {
            var message = new StringBuilder();
            message.AppendLine("Keine Verbindung zu Tankerkönig.de!\n");
            message.Append($"HTTP-Status-Code: {ex.StatusCode.ToString()}");
            ShowWarning(true, message.ToString(), 5000);
        }
        catch (Exception)
        {
            ShowWarning(true, "Keine Verbindung zu Tankerkönig.de!", 5000);
        }
        finally
        {
            ProgressRingIsActive = false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        var settings = new Settings
        {
            TankerkoenigApiKey = TankerKoenigApiKey
        };
        await _settingsWriter!.WriteAsync(settings);
        _mainNavigationService!.Navigate<AddressSelectionViewModel, CustomCrossFadePageTransition>();
    }

    [RelayCommand]
    private void Cancel()
    {
        OnBackPressed();
    }

    #endregion commands

    #region private methods

    private void ShowWarning(bool isError, string message, int duration)
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = null;
        }

        _cancellationTokenSource = new CancellationTokenSource();

        if (isError)
        {
            NoticeTextColor = new SolidColorBrush(Color.Parse("Orange"));
            NoticeTitleText = "Fehler:";
        }
        else
        {
            NoticeTextColor = new SolidColorBrush(Color.Parse("Green"));
            NoticeTitleText = "Erfolg:";
        }

        var token = _cancellationTokenSource.Token;
        Task.Run(async () =>
        {
            NoticeText = message;
            NoticeTextIsVisible = true;

            await Task.Delay(duration, token);

            token.ThrowIfCancellationRequested();

            NoticeTitleText = string.Empty;
            NoticeText = string.Empty;
            NoticeTextIsVisible = false;
        }, token);
    }

    private void OnBackPressed()
    {
        _mainNavigationService!.Navigate<AddressSelectionViewModel, CustomCrossFadePageTransition>();
    }

    #endregion private methods

    #region OnPropertyChanged handlers

    partial void OnTankerKoenigApiKeyChanged(string value)
    {
        ValidateButtonIsEnabled = !string.IsNullOrEmpty(value);
    }

    #endregion OnPropertyChanged handlers

    #region public overrides

    public override void Dispose()
    {
        App.GetCurrent().BackPressed -= OnBackPressed;
    }

    #endregion public overrides
}
using ApiClients;
using ApiClients.Models;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GasPrices.Services;
using GasPrices.Store;
using HttpClient.Exceptions;
using SettingsHandling.Models;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GasPrices.PageTransitions;
using SettingsHandling;
using Xamarin.Essentials;

namespace GasPrices.ViewModels;

public partial class AddressSelectionViewModel : ViewModelBase
{
    #region constructors

    public AddressSelectionViewModel()
    {
    }

    public AddressSelectionViewModel(
        IMapClient mapClient,
        IGasPricesClient gasPricesClient,
        AppStateStore appStateStore,
        ISettingsReader settingsReader,
        ISettingsWriter settingsWriter,
        NavigationService<MainNavigationStore> mainNavigationService)
    {
        _mainNavigationService = mainNavigationService;

        _mapClient = mapClient;
        _appStateStore = appStateStore;
        _gasPricesClient = gasPricesClient;
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;

        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            GeolocationButtonIsVisible = true;
            LocationPickerButtonGridColumn = 1;
        }

        Task.Run(async () =>
        {
            await ProcessApiKeyAsync();
            await ProcessCoordsAsync();
            await ProcessSettingsAsync();
        });
    }

    #endregion constructors

    #region private fields

    private readonly NavigationService<MainNavigationStore>? _mainNavigationService;
    private readonly AppStateStore? _appStateStore;
    private readonly IMapClient? _mapClient;
    private readonly IGasPricesClient? _gasPricesClient;
    private readonly ISettingsReader? _settingsReader;
    private readonly ISettingsWriter? _settingsWriter;

    private Settings? _settings;
    private bool _hasStreetFocus;
    private bool _hasPostalCodeFocus;
    private bool _hasCityFocus;
    private CancellationTokenSource? _cancellationTokenSource;

    #endregion private fields

    #region private properties

    private int RadiusInt
    {
        get
        {
            var isDigit = int.TryParse(Radius, out int intValue);
            return isDigit ? intValue : 5;
        }
    }

    #endregion

    #region bindable properties

    [ObservableProperty] private string _street = string.Empty;
    [ObservableProperty] private string _postalCode = string.Empty;
    [ObservableProperty] private string _city = string.Empty;
    [ObservableProperty] private string _radius = "5";
    [ObservableProperty] private string? _mapCoordinates;
    [ObservableProperty] private bool _mapCoordinatesIsVisible;
    [ObservableProperty] private bool _geolocationButtonIsVisible;
    [ObservableProperty] private bool _geolocationButtonIsEnabled = true;
    [ObservableProperty] private bool _locationPickerButtonIsEnabled = true;
    [ObservableProperty] private int _locationPickerButtonGridColumn;
    [ObservableProperty] private bool _searchButtonIsEnabled = true;
    [ObservableProperty] private bool _progressRingIsActive;
    [ObservableProperty] private bool _warningTextIsVisible;
    [ObservableProperty] private bool _errorTextIsVisible;
    [ObservableProperty] private string _warningText = string.Empty;
    [ObservableProperty] private string _errorText = string.Empty;

    #endregion bindable properties

    #region commands

    [RelayCommand]
    private void StreetFocusChanged(object value) => _hasStreetFocus = value.ToString() == "True";

    [RelayCommand]
    private void PostalCodeFocusChanged(object value) => _hasPostalCodeFocus = value.ToString() == "True";

    [RelayCommand]
    private void CityFocusChanged(object value) => _hasCityFocus = value.ToString() == "True";

    [RelayCommand]
    private async Task DeviceGeolocation()
    {
        MapCoordinates = string.Empty;
        MapCoordinatesIsVisible = false;
        GeolocationButtonIsEnabled = false;
        ProgressRingIsActive = true;

        Location? location = null;

        try
        {
            location = await Geolocation.GetLocationAsync();
        }
        catch (Exception ex)
        {
            var message = new StringBuilder();
            message.AppendLine("Fehler bei der Standortermittlung!\n");
            message.AppendLine("Fehlermeldung:");
            message.Append(ex.Message);
            ShowWarning(message.ToString(), 5000);
        }
        finally
        {
            ProgressRingIsActive = false;
            GeolocationButtonIsEnabled = true;
        }

        if (location != null)
        {
            _appStateStore!.CoordsFromMapClient = new Coords(location.Latitude, location.Longitude);
            await ProcessCoordsAsync();
        }
    }

    [RelayCommand]
    private async Task LocationPicker()
    {
        LocationPickerButtonIsEnabled = false;

        await SaveCurrentAddressAsync();

        try
        {
            _appStateStore!.CoordsFromMapClient = await GetCoordsFromAddressFields();
            _mainNavigationService!.Navigate<LocationPickerViewModel, CustomCrossFadePageTransition>();
        }
        catch (HttpClientException ex)
        {
            var message = new StringBuilder();
            message.AppendLine("Keine Verbindung zum Kartendienst!\n");
            message.AppendLine("Fehlermeldung:");
            message.Append(ex.Message);
            ShowWarning(message.ToString(), 5000);
        }
        catch (BadStatuscodeException ex)
        {
            var message = new StringBuilder();
            message.AppendLine("Keine Verbindung zum Kartendienst!\n");
            message.Append($"HTTP-Status-Code: {ex.StatusCode.ToString()}");
            ShowWarning(message.ToString(), 5000);
        }
        catch (Exception)
        {
            ShowWarning("Keine Verbindung zum Kartendienst!", 5000);
        }
        finally
        {
            LocationPickerButtonIsEnabled = true;
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        if (Radius == "0")
        {
            ShowWarning("Der Umkreis muss mindestens 1 km betragen!", 5000);
            return;
        }

        var isDigit = int.TryParse(Radius, out int _);
        if (!isDigit)
        {
            ShowWarning("Ungültige Eingabe für den Umkreis!", 5000);
            return;
        }

        SearchButtonIsEnabled = false;
        ProgressRingIsActive = true;

        try
        {
            Coords? coords;
            if (_appStateStore!.CoordsFromMapClient != null)
            {
                coords = _appStateStore.CoordsFromMapClient;
            }
            else
            {
                coords = await GetCoordsFromAddressFields();
            }

            if (coords != null)
            {
                var stations =
                    await _gasPricesClient?.GetStationsAsync(_settings!.TankerkoenigApiKey!, coords, RadiusInt)!;

                if (stations is { Count: > 0 })
                {
                    _appStateStore!.Stations = stations;
                    await SaveCurrentAddressAsync();
                    _appStateStore.CoordsFromMapClient = null;
                    _mainNavigationService?.Navigate<ResultsViewModel, CustomCrossFadePageTransition>();
                }
                else
                {
                    ShowWarning("Es wurden keine Tankstellen gefunden!", 5000);
                }
            }
            else
            {
                ShowWarning("Es wurde keine gültige Adresse eingegeben!", 5000);
            }
        }
        catch (HttpClientException ex)
        {
            var message = new StringBuilder();
            message.AppendLine("Fehler bei der Tankstellensuche!\n");
            message.AppendLine("Fehlermeldung:");
            message.Append(ex.Message);
            ShowWarning(message.ToString(), 5000);
        }
        catch (BadStatuscodeException ex)
        {
            var message = new StringBuilder();
            message.AppendLine("Fehler bei der Tankstellensuche!\n");
            message.Append($"HTTP-Status-Code: {ex.StatusCode.ToString()}");
            ShowWarning(message.ToString(), 5000);
        }
        catch (Exception)
        {
            ShowWarning("Bei der Abfrage der Tankstellen ist ein Fehler aufgetreten!", 5000);
        }
        finally
        {
            ProgressRingIsActive = false;
            SearchButtonIsEnabled = true;
        }
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        await SaveCurrentAddressAsync();
        _mainNavigationService?.Navigate<SettingsViewModel, CustomCrossFadePageTransition>();
    }

    [RelayCommand]
    private Task KeyDown(object o)
    {
        var e = o as KeyEventArgs;
        if (e?.Key is not (Key.Enter or Key.Return)) return Task.CompletedTask;
        if (!SearchButtonIsEnabled) return Task.CompletedTask;
        e.Handled = true;
        return Search();
    }

    #endregion commands

    #region OnPropertyChangedHandlers

    partial void OnStreetChanged(string value) => ResetCachedCoords();
    partial void OnCityChanged(string value) => ResetCachedCoords();
    partial void OnPostalCodeChanged(string value) => ResetCachedCoords();

    #endregion OnPropertyChangedHandlers

    #region private methods

    private async Task ProcessApiKeyAsync()
    {
        _settings = await _settingsReader!.ReadAsync();
        if (_settings == null || string.IsNullOrEmpty(_settings?.TankerkoenigApiKey))
        {
            SearchButtonIsEnabled = false;
            var warning = new StringBuilder();
            warning.Append("Es wurde kein Tankerkönig-API-Schlüssel gefunden!\n\n");
            warning.Append("Bitte zu den Einstellungen wechseln.");
            WarningText = warning.ToString();
            WarningTextIsVisible = true;
        }
    }

    private async Task ProcessSettingsAsync()
    {
        _settings = await _settingsReader?.ReadAsync()!;

        if (_appStateStore?.Distance != null)
        {
            Radius = _appStateStore.Distance.Value.ToString();
        }
        else if (_settings is { LastKnownRadius: not null })
        {
            Radius = _settings.LastKnownRadius;
        }

        if (_appStateStore?.CoordsFromMapClient != null) return;

        if (_settings is { LastKnownLatitude: not null, LastKnownLongitude: not null })
        {
            _appStateStore!.CoordsFromMapClient = new Coords(
                _settings.LastKnownLatitude.Value, _settings.LastKnownLongitude.Value);
            MapCoordinates = _appStateStore.CoordsFromMapClient!.ToString();
            MapCoordinatesIsVisible = true;
        }

        if (_appStateStore?.Address != null)
        {
            Street = _appStateStore.Address.Street!;
            PostalCode = _appStateStore.Address.PostalCode!;
            City = _appStateStore.Address.City!;
        }
        else if (_settings != null)
        {
            Street = _settings.LastKnownStreet ?? string.Empty;
            PostalCode = _settings.LastKnownPostalCode ?? string.Empty;
            City = _settings.LastKnownCity ?? string.Empty;
        }
    }

    private async Task ProcessCoordsAsync()
    {
        if (_appStateStore?.CoordsFromMapClient == null)
        {
            return;
        }

        ProgressRingIsActive = true;

        Address? address = null;

        try
        {
            address = await _mapClient?.GetAddressAsync(_appStateStore.CoordsFromMapClient!)!;
        }
        catch (HttpClientException ex)
        {
            var message = new StringBuilder();
            message.AppendLine("Fehler beim Auswerten der Koordinaten!\n");
            message.AppendLine("Fehlermeldung:");
            message.Append(ex.Message);
            ShowWarning(message.ToString(), 5000);
        }
        catch (BadStatuscodeException ex)
        {
            var message = new StringBuilder();
            message.AppendLine("Fehler beim Auswerten der Koordinaten!\n");
            message.Append($"HTTP-Status-Code: {ex.StatusCode.ToString()}");
            ShowWarning(message.ToString(), 5000);
        }
        catch (Exception)
        {
            ShowWarning("Fehler beim Auswerten der Koordinaten!", 5000);
        }
        finally
        {
            ProgressRingIsActive = false;
        }

        var isWrongPosition = false;
        var wrongPosWarningMsg = new StringBuilder();
        if (address == null)
        {
            isWrongPosition = true;
            wrongPosWarningMsg.AppendLine("Es konnte keine Addresse zu der gewünschten Position ermittelt werden.");
            wrongPosWarningMsg.Append("Bitte eine andere Position oder Adresse wählen.");
        }
        else if (address.Country != "Deutschland")
        {
            isWrongPosition = true;
            wrongPosWarningMsg.AppendLine("Diese App kann nur Tankstellen in Deutschland anzeigen.");
            wrongPosWarningMsg.Append("Bitte eine andere Position oder Adresse wählen.");
        }
        else
        {
            MapCoordinates = _appStateStore.CoordsFromMapClient!.ToString();
            MapCoordinatesIsVisible = true;
            _appStateStore.Address = address;
            Street = _appStateStore.Address?.Street!;
            PostalCode = _appStateStore.Address?.PostalCode!;
            City = _appStateStore.Address?.City!;
        }

        if (isWrongPosition)
        {
            ShowWarning(wrongPosWarningMsg.ToString(), 5000);
        }
    }

    private async Task SaveCurrentAddressAsync(Coords? coords = null)
    {
        var address = new Address(Street, City, PostalCode);
        _appStateStore!.Address = address;
        _appStateStore.Distance = RadiusInt;

        var settings = await _settingsReader?.ReadAsync()!;
        settings ??= new Settings();

        settings.LastKnownStreet = Street;
        settings.LastKnownCity = City;
        settings.LastKnownPostalCode = PostalCode;
        settings.LastKnownRadius = RadiusInt.ToString();

        if (_appStateStore.CoordsFromMapClient != null)
        {
            settings.LastKnownLatitude = _appStateStore.CoordsFromMapClient?.Latitude;
            settings.LastKnownLongitude = _appStateStore.CoordsFromMapClient?.Longitude;
        }
        else
        {
            if (coords != null)
            {
                settings.LastKnownLatitude = coords.Latitude;
                settings.LastKnownLongitude = coords.Longitude;
            }
            else
            {
                settings.LastKnownLatitude = null;
                settings.LastKnownLongitude = null;
            }
        }

        await _settingsWriter!.WriteAsync(settings);
    }

    private void ResetCachedCoords()
    {
        if (!_hasStreetFocus && !_hasPostalCodeFocus && !_hasCityFocus) return;
        _appStateStore!.CoordsFromMapClient = null;
        MapCoordinates = null;
        MapCoordinatesIsVisible = false;
    }

    private async Task<Coords?> GetCoordsFromAddressFields()
    {
        var address = new Address(Street, City, PostalCode);
        var coords = await _mapClient?.GetCoordsAsync(address)!;

        return coords;
    }

    private void ShowWarning(string message, int duration)
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = null;
        }

        _cancellationTokenSource = new CancellationTokenSource();

        var token = _cancellationTokenSource.Token;
        Task.Run(async () =>
        {
            WarningText = message;
            WarningTextIsVisible = true;

            await Task.Delay(duration, token);

            token.ThrowIfCancellationRequested();

            WarningText = string.Empty;
            WarningTextIsVisible = false;
        }, token);
    }

    #endregion private methods

    #region public overrides

    public override void Dispose()
    {
    }

    #endregion public overrides
}
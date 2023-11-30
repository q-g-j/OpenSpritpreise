﻿using System;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GasPrices.PageTransitions;
using GasPrices.Services;
using GasPrices.Store;

namespace GasPrices.ViewModels;

public partial class LocationPickerViewModel : ViewModelBase
{
    #region constructors

    public LocationPickerViewModel()
    {
    }

    public LocationPickerViewModel(
        NavigationService<MainNavigationStore> mainNavigationService,
        AppStateStore appStateStore)
    {
        _mainNavigationService = mainNavigationService;
        _appStateStore = appStateStore;

        ApplyButtonIsEnabled = _appStateStore.CoordsFromMapClient != null;

        if (OperatingSystem.IsAndroid())
        {
            BackButtonIsVisible = false;
        }

        ((App)Application.Current!).BackPressed += OnBackPressed;
    }

    #endregion constructors

    #region private fields

    private readonly NavigationService<MainNavigationStore>? _mainNavigationService;
    private readonly AppStateStore? _appStateStore;

    #endregion private fields

    #region bindable properties

    [ObservableProperty] private bool _applyButtonIsEnabled;
    [ObservableProperty] private bool _backButtonIsVisible = true;

    #endregion bindable properties

    #region commands

    [RelayCommand]
    public void ApplyCommand()
    {
        _mainNavigationService?.Navigate<AddressSelectionViewModel, CustomCrossFadePageTransition>();
    }

    [RelayCommand]
    public void BackCommand()
    {
        OnBackPressed();
    }

    #endregion commands

    #region private methods

    private void OnBackPressed()
    {
        _appStateStore!.CoordsFromMapClient = null;
        _mainNavigationService!.Navigate<AddressSelectionViewModel, CustomCrossFadePageTransition>();
    }

    #endregion private methods

    #region public overrides

    public override void Dispose()
    {
        ((App)Application.Current!).BackPressed -= OnBackPressed;
    }

    #endregion public overrides
}
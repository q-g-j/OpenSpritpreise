using System;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GasPrices.Services;
using GasPrices.Store;
using GasPrices.PageTransitions;

namespace GasPrices.ViewModels;

public partial class ResultsViewModel : ViewModelBase
{
    #region constructors

    public ResultsViewModel()
    {
    }

    public ResultsViewModel(
        ResultsNavigationStore resultsNavigationStore,
        NavigationService<MainNavigationStore> mainNavigationService,
        NavigationService<ResultsNavigationStore> resultsNavigationService)
    {
        _resultsNavigationStore = resultsNavigationStore;
        _mainNavigationService = mainNavigationService;
        _resultsNavigationService = resultsNavigationService;

        _resultsNavigationStore.CurrentViewModelChanged += () =>
        {
            CurrentPageTransition = _resultsNavigationStore!.CurrentPageTransition;
            CurrentViewModel = _resultsNavigationStore.CurrentViewModel;
        };

        _resultsNavigationService
            .Navigate<StationListViewModel,
                CustomCompositePageTransition<CustomCrossFadePageTransition, SlideLeftPageTransition>>();

        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            BackButtonIsVisible = false;
        }

        App.GetCurrent().BackPressed += OnBackPressed;
    }

    #endregion constructors

    #region private fields

    private readonly ResultsNavigationStore? _resultsNavigationStore;
    private readonly NavigationService<MainNavigationStore>? _mainNavigationService;
    private readonly NavigationService<ResultsNavigationStore>? _resultsNavigationService;

    #endregion privat fields

    #region bindable properties

    [ObservableProperty] private ViewModelBase? _currentViewModel;
    [ObservableProperty] private ICustomPageTransition? _currentPageTransition;
    [ObservableProperty] private bool _backButtonIsVisible = true;

    #endregion bindable properties

    #region commands

    [RelayCommand]
    private void Back()
    {
        OnBackPressed();
    }

    #endregion commands

    #region private methods

    private void OnBackPressed()
    {
        if (CurrentViewModel!.GetType() == typeof(StationDetailsViewModel))
        {
            _resultsNavigationService!
                .Navigate<StationListViewModel,
                    CustomCompositePageTransition<CustomCrossFadePageTransition, SlideRightPageTransition>>();
        }
        else
        {
            _mainNavigationService!.Navigate<AddressSelectionViewModel, CustomCrossFadePageTransition>();
        }
    }

    #endregion private methods

    #region public overrides

    public override void Dispose()
    {
        App.GetCurrent().BackPressed -= OnBackPressed;
    }

    #endregion public overrides
}
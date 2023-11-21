﻿using Avalonia.Controls.Templates;
using Avalonia.Controls;
using GasPrices.ViewModels;
using System;

namespace GasPrices.Services
{
    public class ViewLocatorService(Func<Type, Control> viewCreator) : IDataTemplate
    {
        private readonly Func<Type, Control> _viewCreator = viewCreator;

        public static bool SupportsRecycling => false;

        public Control Build(object? data)
        {
            string? name = data?.GetType().FullName?.Replace("ViewModel", "View");
            Type? type = Type.GetType(name!);

            if (type != null)
            {
                return _viewCreator(type);
            }
            else
            {
                return new TextBlock { Text = "Not Found: " + name };
            }
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
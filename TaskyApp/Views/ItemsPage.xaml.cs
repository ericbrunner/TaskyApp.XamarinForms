using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskyApp.Models;
using TaskyApp.ViewModels;
using TaskyApp.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TaskyApp.Views
{
    public partial class ItemsPage : ContentPage
    {
        ItemsViewModel _viewModel;

        public ItemsPage()
        {
            InitializeComponent();

            BindingContext = _viewModel = new ItemsViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                await _viewModel.OnAppearing();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{DateTime.Now:O}-{nameof(ItemsPage)}.{nameof(OnAppearing)} Exception: {e.Message}");
            }

        }
    }
}
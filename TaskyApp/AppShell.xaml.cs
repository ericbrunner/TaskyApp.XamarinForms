using System;
using System.Collections.Generic;
using TaskyApp.ViewModels;
using TaskyApp.Views;
using Xamarin.Forms;

namespace TaskyApp
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
            Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));
        }

    }
}

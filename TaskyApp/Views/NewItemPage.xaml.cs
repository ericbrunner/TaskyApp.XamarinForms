using System;
using System.Collections.Generic;
using System.ComponentModel;
using TaskyApp.Models;
using TaskyApp.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TaskyApp.Views
{
    public partial class NewItemPage : ContentPage
    {
        public Todo TodoItem { get; set; }

        public NewItemPage()
        {
            InitializeComponent();
            BindingContext = new NewItemViewModel();
        }
    }
}
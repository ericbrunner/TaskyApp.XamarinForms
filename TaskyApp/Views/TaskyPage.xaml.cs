using System;
using System.ComponentModel;
using TaskyApp.Contracts;
using TaskyApp.ViewModels;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TaskyApp.Views
{
    public partial class TaskyPage : ContentPage
    {
        public TaskyPage()
        {
            InitializeComponent();

            BindingContext = App.Get<ITaskyViewModel>();
        }
    }
}
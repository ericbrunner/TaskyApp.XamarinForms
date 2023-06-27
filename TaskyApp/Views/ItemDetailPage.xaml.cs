using System.ComponentModel;
using TaskyApp.ViewModels;
using Xamarin.Forms;

namespace TaskyApp.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}
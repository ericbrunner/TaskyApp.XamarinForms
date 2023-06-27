using System;
using Microsoft.Extensions.DependencyInjection;
using TaskyApp.Contracts;
using TaskyApp.Models;
using TaskyApp.Services;
using TaskyApp.Tasky;
using TaskyApp.ViewModels;
using TaskyApp.Views;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TaskyApp
{
    public partial class App : Application
    {
        public static TImplementation? Get<TImplementation>() where TImplementation : class => 
            ServiceProvider.GetService(typeof(TImplementation)) as TImplementation;

        public static IServiceProvider ServiceProvider { get; private set; } = default!;

        public App(Action<IServiceCollection> registerPlatformAction)
        {
            InitializeComponent();

            #region Register IoC container services
            var services= new ServiceCollection();

            registerPlatformAction?.Invoke(services);

            services.AddSingleton<IDataStore<Todo>, TodosDataStore>();


            services.AddTransient<ITaskyViewModel, TaskyViewModel>();

            ServiceProvider = services.BuildServiceProvider(options:new ServiceProviderOptions(){ValidateOnBuild = true});
            #endregion

            MainPage = new AppShell();
        }



        protected override void OnStart()
        {
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now:O}-{nameof(OnStart)} invoked");

            if (!Preferences.Get(TaskyViewModel.GeoLocationWorkloadName, defaultValue: false)) return;
            
            var taskyViewModel = Get<ITaskyViewModel>();
            taskyViewModel?.StartGpsServiceCommand.Execute(null);
        }

        protected override void OnSleep()
        {
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now:O}-{nameof(OnSleep)} invoked");
        }

        protected override void OnResume()
        {
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now:O}-{nameof(OnResume)} invoked");
        }
    }
}

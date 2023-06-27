using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;

namespace TaskyApp.Contracts;

public interface ITaskyViewModel : IBaseViewModel
{
    IAsyncCommand GetTodosCommand { get; }
    ICommand GetLocationCommand { get; }
    ICommand StartGpsServiceCommand { get; }
    ICommand StopGpsServiceCommand { get; }
    ICommand StartWorkerCommand { get; }
    ICommand StopWorkerCommand { get; }
    ICommand StartTaskCommand { get; }
    ICommand StopTaskCommand { get; }
}
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskyApp.Contracts;
using TaskyApp.Models;
using TaskyApp.Tasky.Messages;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TaskyApp.ViewModels;

public class TaskyViewModel : BaseViewModel, ITaskyViewModel
{
    private readonly ITaskRunner _taskRunner;

    public TaskyViewModel(ITaskRunner taskRunner)
    {
        Title = "Tasky";

        _taskRunner = taskRunner;

        GetLocationCommand = new AsyncCommand(GetLocation);
        GetTodosCommand = new AsyncCommand(FetchTodos);

        StartTaskCommand = new AsyncCommand(StartTask);
        StopTaskCommand = new AsyncCommand(StopTask);

        StartWorkerCommand = new AsyncCommand(StartWorker);
        StopWorkerCommand = new AsyncCommand(StopWorker);


        StartGpsServiceCommand = new AsyncCommand(StartGpsService);
        StopGpsServiceCommand = new Command(StopGpsService);
    }

    #region Get Todos

    public IAsyncCommand GetTodosCommand { get; }

    private async Task FetchTodos()
    {
        var todoRepo = App.Get<IDataStore<Todo>>();

        if (todoRepo == null)
        {
            Debug.Write($"{DateTime.Now:O}-{nameof(TaskyViewModel)}.{nameof(FetchTodos)} {nameof(todoRepo)} is null.");
        }
        var result = await todoRepo.GetItemsAsync();

        Debug.WriteLine(
            $"{DateTime.Now:O}-{nameof(TaskyViewModel)}.{nameof(FetchTodos)} Total Todo Items: {result.Count()}");
    }

    #endregion

    #region Get Location

    public ICommand GetLocationCommand { get; }

    private async Task GetLocation()
    {
        if (!await CheckGeolocationPermission()) return;

        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(15));
            var location = await Geolocation.GetLocationAsync(request);

            if (location != null)
            {
                Debug.WriteLine(
                    $"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");
            }
        }

        catch (Exception e)
        {
            _taskRunner.Log($"{e.Message}");
        }
    }

    #endregion

    #region Mode: Service (droid) - Workload: GeoLocationWorkloadName

    public ICommand StartGpsServiceCommand { get; }
    public ICommand StopGpsServiceCommand { get; }

    public const string GeoLocationWorkloadName = nameof(GeoLocationWorkloadName);

    private void StopGpsService()
    {
        _taskRunner.StopService(GeoLocationWorkloadName);
    }

    private async Task StartGpsService()
    {
        if (!await CheckGeolocationPermission()) return;

        _geoLocindex = 0;
        await _taskRunner.StartService(FetchGeolocation, GeoLocationWorkloadName, TimeSpan.FromMilliseconds(5000));
    }

    private async Task<bool> CheckGeolocationPermission()
    {
        try
        {
            await _taskRunner.EnsureGrantedPermission<Permissions.LocationAlways>();
        }
        catch (PermissionException pe)
        {
            _taskRunner.Log($"{pe.Message}");
            await MainThread.InvokeOnMainThreadAsync(() => Application.Current.MainPage.DisplayAlert(
                "Permission Error",
                "Please goto Settings->TaskyApp and set Location Permission to Always", "Ok"));

            Xamarin.Essentials.AppInfo.ShowSettingsUI();

            return false;
        }

        return true;
    }

    #endregion

    #region Mode: Worker (droid), BackgroundTask (iOS)

    public ICommand StartWorkerCommand { get; }
    public ICommand StopWorkerCommand { get; }

    private CancellationTokenSource? _workerCts;
    private int _workerIndex;
    private Task? _workerTask;

    private async Task StartWorker()
    {
        if (!await CheckGeolocationPermission()) return;

        _workerIndex = 0;
        _workerCts = new CancellationTokenSource();
        MessagingCenter.Subscribe<GenericListenableWorkerMessage, Task>(this, "TaskCallback",
            (s, e) => { _workerTask = e; });

        _taskRunner.RunWorker(async (cancellationToken) =>
        {
            Debug.Write($"{DateTime.Now:O}-{nameof(TaskyViewModel)}.{nameof(StartWorker)}.BackgroundWork started");

            await DoWork(cancellationToken);

            Debug.Write($"{DateTime.Now:O}-{nameof(TaskyViewModel)}.{nameof(StartWorker)}.BackgroundWork finished");
        }, _workerCts.Token, TimeSpan.FromMilliseconds(5000));
    }

    #region Work

    private int _geoLocindex;

    public async Task FetchGeolocation(string logTag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(2));
            Location? location = await Geolocation.GetLocationAsync(request, cancellationToken);

            var position = location != null
                ? $"{location.Latitude}/{location.Longitude} lat/lon"
                : string.Empty;
            _taskRunner.Log($"invoked {++_geoLocindex} times with {position}", logTag);
        }
        catch (Exception ex)
        {
            _taskRunner?.Log(
                $"{DateTime.Now:O}-{nameof(TaskyViewModel)}.{nameof(FetchGeolocation)} Exception: {ex.Message}");
        }
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        _taskRunner.Log("Started");

        //1st workload: get current geolocation 
        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
        var location = await Geolocation.GetLocationAsync(request, cancellationToken);

        var gpsPositon = location != null ? $"with {location.Latitude}/{location.Longitude} lat/lon" : string.Empty;
        _taskRunner.Log($"{DateTime.Now:O}-{nameof(DoWork)} invoked {++_workerIndex} times {gpsPositon}");

        //2nd workload: fetch some todos form a REST Api endpoint
        await FetchTodos();


        _taskRunner.Log("Finished");
    }

    #endregion

    private async Task StopWorker()
    {
        _workerCts?.Cancel();

        try
        {
            if (_workerTask != null)
            {
                await _workerTask;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine(
                $"{DateTime.Now:O}-{nameof(TaskyViewModel)}.{nameof(StopWorker)} Stopped worker job.");
        }
        catch (Exception e)
        {
            if (e is AggregateException aggregateException)
            {
                var flattenedException = aggregateException.Flatten();

                foreach (var innerException in flattenedException.InnerExceptions)
                {
                    Debug.WriteLine(
                        $"{DateTime.Now:O}-{nameof(StopWorker)} Flattened Inner Exception: {innerException.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"{DateTime.Now:O}-{nameof(StopWorker)} Exception: {e.Message}");
            }
        }
        finally
        {
            MessagingCenter.Unsubscribe<GenericListenableWorkerMessage, Task>(this, "TaskCallback");
        }
    }

    #endregion

    #region Mode: .NET Task (droid + iOS)

    public ICommand StartTaskCommand { get; }
    public ICommand StopTaskCommand { get; }

    private CancellationTokenSource? _taskCts;
    private Task? _runTask;

    private async Task StartTask()
    {
        if (!await CheckGeolocationPermission()) return;

        _workerIndex = 0;
        _taskCts = new CancellationTokenSource();


        _runTask = _taskRunner.RunTask(DoWork, _taskCts.Token, TimeSpan.FromMilliseconds(3000));
    }

    private async Task StopTask()
    {
        _taskCts?.Cancel();

        try
        {
            if (_runTask != null)
            {
                await _runTask;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine(
                $"{DateTime.Now:O}-{nameof(TaskyViewModel)}.{nameof(StopTask)} Stopped .NET Task.");
        }
        catch (Exception e)
        {
            if (e is AggregateException aggregateException)
            {
                AggregateException flattenedException = aggregateException.Flatten();

                foreach (var innerException in flattenedException.InnerExceptions)
                {
                    Debug.WriteLine(
                        $"{DateTime.Now:O}-{nameof(StopTask)} Flattened Inner Exception: {innerException.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"{DateTime.Now:O}-{nameof(StopTask)} Exception: {e.Message}");
            }
        }
    }

    #endregion
}
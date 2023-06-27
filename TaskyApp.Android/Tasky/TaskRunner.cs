using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Work;
using Java.Util;
using TaskyApp.Contracts;
using TaskyApp.Tasky;
using Xamarin.Essentials;
using Debug = System.Diagnostics.Debug;
using Exception = System.Exception;

namespace TaskyApp.Droid.Tasky
{
    public class TaskRunner : ITaskRunner
    {
        public Task RunTask(Func<CancellationToken, Task> backgroundTask,
            CancellationToken cancellationToken,
            TimeSpan? interval = null)
        {
            return Task.Run(async () =>
            {
                if (interval == null)
                {
                    await backgroundTask(cancellationToken);
                }
                else
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await backgroundTask(cancellationToken);

                        await Task.Delay(interval.Value, cancellationToken);
                    }
                }
            }, cancellationToken);
        }

        private OneTimeWorkRequest? _syncWorkerRequest;

        public void RunWorker(Func<CancellationToken, Task> backgroundTask,
            CancellationToken cancellationToken,
            TimeSpan? interval = null)
        {
            var workerBag = new WorkerBag
            {
                BackgroundTask = backgroundTask,
                CancellationToken = cancellationToken,
                Interval = interval
            };
            GenericListenableWorker.WorkerBag = workerBag;

            _syncWorkerRequest = new OneTimeWorkRequest
                    .Builder(typeof(GenericListenableWorker))
                .AddTag(GenericListenableWorker.Tag)
                .Build();

            WorkManager.GetInstance(Platform.CurrentActivity)
                .BeginUniqueWork(GenericListenableWorker.Tag, ExistingWorkPolicy.Replace, _syncWorkerRequest)
                .Enqueue();
        }

        public Task CancelWorkById()
        {
            if (_syncWorkerRequest == null)
                throw new InvalidOperationException($"{nameof(_syncWorkerRequest)} is null.");

            var workId = _syncWorkerRequest.Id;
            var op = WorkManager.GetInstance(Platform.CurrentActivity).CancelWorkById(workId);

            return Task.CompletedTask;
        }

        #region Logging

        public void Log(string message, string tag = nameof(ITaskRunner), Exception? exception = null,
            [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFileName = "")
        {
            var callerTypeName = string.Empty;
            try
            {
                callerTypeName = System.IO.Path.GetFileNameWithoutExtension(callerFileName).Split('\\').LastOrDefault();

            }
            catch (Exception e)
            {
                message = e.ToString();
            }

            message = $"Thread-{Thread.CurrentThread.ManagedThreadId}-{callerTypeName}.{callerMemberName} {message}";

#if (DEBUG == true)
            //Android.Util.Log.Debug(tag, message);
            Android.Util.Log.Info(tag, message);
#endif
            Debug.WriteLine($"{DateTime.Now:O}- {message}");

            if (IsPingPongServerEnabled)
            {
                RunSync(() => ContactPingPongServer($"{DateTime.Now:O}- {message}"));
            }
        }

        #region Ping-Pong TCP/IP Server for LogCat transfer within WLAN from Phone to PC (Analytics)

        private void RunSync(Func<Task> func)
        {
            try
            {
                Task.Run(func).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch
            {
                // Ignored
            }
        }
        public bool IsPingPongServerEnabled { get; } = true;
        private async Task ContactPingPongServer(string message)
        {
            try
            {
                var serverIp = "192.168.0.240";
                IPAddress serverIpAddress = IPAddress.Parse(serverIp);
                const int serverPort = 1234;
                using var tcpClient = new TcpClient();

                await tcpClient.ConnectAsync(serverIpAddress, serverPort);

                Debug.WriteLine(
                    $"{DateTime.Now:O}-{nameof(TaskRunner)}.{nameof(ContactPingPongServer)} connected to server :-)");


                // Translate the passed message into ASCII and store it as a Byte array.
                byte[] data = System.Text.Encoding.ASCII.GetBytes($"PING: {message}");

                // Get a client stream for reading and writing.
                NetworkStream stream = tcpClient.GetStream();

                // -- Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);

                Debug.WriteLine(
                    $"{DateTime.Now:O}-{nameof(TaskRunner)}.{nameof(ContactPingPongServer)} - Sent: {message}");

                // ----- Receive the server response.

                // Buffer to store the response bytes.
                data = new byte[4096];

                // Read the first batch of the TcpServer response bytes.
                int bytes = stream.Read(data, 0, data.Length);
                var responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Debug.WriteLine(
                    $"{DateTime.Now:O}-{nameof(TaskRunner)}.{nameof(ContactPingPongServer)} - Received: {responseData}");
            }
            catch (Exception e)
            {
                Debug.WriteLine(
                    $"{DateTime.Now:O}-{nameof(TaskRunner)}.{nameof(ContactPingPongServer)} Exception: {e.Message} Type: {e.GetType().Name}");
            }
        }

        #endregion

        private PowerManager.WakeLock? _cpuWakeLock;
        public void AquireCpuWakeLock(string logTag)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) return;
            
            if (Application.Context.GetSystemService(Context.PowerService) is PowerManager pm)
            {
                _cpuWakeLock = pm.NewWakeLock(WakeLockFlags.Partial, GpsConstants.TaskRunnerServiceWakeLock);
                _cpuWakeLock?.Acquire();
                Log("Wakelock aquired", logTag);
            }
        }

        public void ReleaseCpuWakeLock(string logTag)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) return;

            if (_cpuWakeLock is { IsHeld: true })
            {
                _cpuWakeLock.Release();
                Log("Wakelock release", logTag);
            }
        }

        #endregion

        #region Permission Check

        public Task EnsureGrantedPermission<TPermission>() where TPermission : Permissions.BasePermission, new()
        {
            return MainThread.IsMainThread
                ? InnerCheckGeolocationPermission<TPermission>()
                : MainThread.InvokeOnMainThreadAsync(InnerCheckGeolocationPermission<TPermission>);
        }

        private async Task InnerCheckGeolocationPermission<TPermission>() where TPermission : Permissions.BasePermission, new()
        {
            var permissionStatus = await Permissions.CheckStatusAsync<TPermission>();

            if (permissionStatus == PermissionStatus.Granted)
            {
                Log($"Check Permission '{typeof(TPermission).Name}' Ok: {permissionStatus}");
                return;
            }

            if (Permissions.ShouldShowRationale<TPermission>())
            {
                throw new PermissionException($"{typeof(TPermission).Name} permission was not granted: {permissionStatus}");
            }

            // Permissions.LocationAlways: Is required to get GPS coords when app is in background
            var permission = await Permissions.RequestAsync<TPermission>();
            Log($"Requested Permission '{typeof(TPermission).Name}' Status: {permissionStatus}");

            if (permission == PermissionStatus.Granted)
            {
                return;
            }

            throw new PermissionException($"{typeof(TPermission).Name} permission was not granted: {permissionStatus}");
        }

        #endregion


        #region Run in Service Mode

        public Func<string, CancellationToken, Task>? DoWorkFunc { get; private set; }

        public Task StartService(Func<string, CancellationToken, Task> serviceFunc,
            string workloadName, TimeSpan? interval = null)
        {
            if (serviceFunc == null) throw new ArgumentNullException(nameof(serviceFunc));

            if (IsServiceRunning(typeof(TaskRunnerService)))
            {
                Debug.WriteLine(
                    $"{DateTime.Now:O}-{nameof(TaskRunner)}.{nameof(StartService)} service already started. " +
                    $"Call StopService.");
                return Task.CompletedTask;
            }

            Preferences.Set(workloadName, true);

            
            var startServiceIntent = new Intent(Platform.CurrentActivity, typeof(TaskRunnerService));
            DoWorkFunc = serviceFunc;


            startServiceIntent.PutExtra("interval", interval?.Seconds ?? 0);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                Platform.CurrentActivity.StartForegroundService(
                    startServiceIntent);
            }
            else
            {
                Platform.CurrentActivity.StartService(startServiceIntent);
            }

            return Task.CompletedTask;
        }

        public Task<bool> StopService(string workloadName)
        {
            var result = InnerStopService();

            Preferences.Set(workloadName, false);
            return result;
        }


        private Task<bool> InnerStopService()
        {
            if (IsServiceRunning(typeof(TaskRunnerService)))
            {
                var result = Platform.CurrentActivity.StopService(new Intent(
                    Platform.CurrentActivity,
                    typeof(TaskRunnerService)));

                return Task.FromResult(result);
            }

            return Task.FromResult(true);
        }

        private bool IsServiceRunning(Type cls)
        {
            ActivityManager? manager =
               Platform.CurrentActivity.GetSystemService(Context.ActivityService) as ActivityManager;
            if (manager == null) return false;

            foreach (var service in manager.GetRunningServices(int.MaxValue))
            {
                if (service.Service.ClassName.Equals(Java.Lang.Class.FromType(cls).CanonicalName))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }

    public class WorkerBag : Java.Lang.Object
    {
        public Func<CancellationToken, Task>? BackgroundTask { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public TimeSpan? Interval { get; set; }
        public Task? Task { get; set; }
    }
}
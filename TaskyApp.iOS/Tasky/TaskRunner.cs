using CoreFoundation;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using TaskyApp.Contracts;
using TaskyApp.Tasky;
using TaskyApp.Tasky.Messages;
using UIKit;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TaskyApp.iOS.Tasky
{
    public class TaskRunner : ITaskRunner
    {
        private readonly CoreFoundation.OSLog _loggerInstance;
        public event EventHandler<TaskRunnerEventArgs>? StatusChanged;

        public bool IsPingPongServerEnabled { get; } = true;


        public TaskRunner()
        {
            var subsystem = NSBundle.MainBundle.BundleIdentifier;
            _loggerInstance = new(subsystem: subsystem, category: "taskrunner");
        }

        public Task RunTask(Func<CancellationToken, Task> backgroundTask, CancellationToken cancellationToken,
            TimeSpan? interval = null)
        {
            return Task.Run(async () =>
            {
                try
                {
                    OnStatusChanged(BackgroundTaskStatus.Running);

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

                    OnStatusChanged(BackgroundTaskStatus.Completed);
                }
                catch (Exception ex)
                {
                    OnStatusChanged(BackgroundTaskStatus.Failed, ex);
                }
            }, cancellationToken);
        }

        private void OnStatusChanged(BackgroundTaskStatus status, Exception? ex = null)
        {
            StatusChanged?.Invoke(this, new TaskRunnerEventArgs(status, ex));
        }

        public void RunWorker(Func<CancellationToken, Task> backgroundTask,
            CancellationToken cancellationToken,
            TimeSpan? interval = null)
        {
            Task workerTask = Task.Run(async () =>
            {
                if (interval == null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                else
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        nint taskId = 0;
                        taskId = UIApplication.SharedApplication.BeginBackgroundTask(() =>
                        {
                            Debug.WriteLine(
                                $"{DateTime.Now:O}-{nameof(TaskRunner)}.{nameof(RunWorker)} Time execution limit reached. Stopping the background task.");
                            // ReSharper disable once AccessToModifiedClosure - we need the new taskId value here, not the initial 0.
                            UIApplication.SharedApplication.EndBackgroundTask(taskId);

                            cancellationToken.ThrowIfCancellationRequested();
                        });

                        await backgroundTask(cancellationToken);

                        UIApplication.SharedApplication.EndBackgroundTask(taskId);

                        await Task.Delay(interval.Value, cancellationToken);
                    }
                }
            }, cancellationToken);

            MessagingCenter.Send(new GenericListenableWorkerMessage(), "TaskCallback", workerTask);
        }

        public Func<string, CancellationToken, Task>? DoWorkFunc { get; set; }
        private CancellationTokenSource? _workerCts;
        private Task? _workerTask;
        private const string LogTag = $"X:Time4App-{nameof(TaskRunner)}";

        public Task StartService(Func<string, CancellationToken, Task> serviceFunc,
            string workloadName,
            TimeSpan? interval = null)
        {
            if (serviceFunc == null) throw new ArgumentNullException(nameof(serviceFunc));

            Preferences.Set(workloadName, true);

            _workerCts = new CancellationTokenSource();
            MessagingCenter.Subscribe<GenericListenableWorkerMessage, Task>(this, "TaskCallback",
                (_, e) => { _workerTask = e; });

            RunWorker(cancellationToken => serviceFunc(LogTag, cancellationToken), _workerCts.Token, interval);

            return Task.CompletedTask;
        }

        public async Task<bool> StopService(string workloadName)
        {
            bool result = false;

            _workerCts?.Cancel();

            try
            {
                if (_workerTask != null)
                {
                    await _workerTask;
                }

                result = true;
            }
            catch (OperationCanceledException)
            {
                Log("Stopped worker job.");
            }
            catch (Exception e)
            {
                if (e is AggregateException aggregateException)
                {
                    var flattenedException = aggregateException.Flatten();

                    foreach (var innerException in flattenedException.InnerExceptions)
                    {
                        Log($"Flattened Inner Exception: {innerException.Message}");
                    }
                }
                else
                {
                    Debug.WriteLine($"Exception: {e.Message}");
                }
            }
            finally
            {
                MessagingCenter.Unsubscribe<GenericListenableWorkerMessage, Task>(this, "TaskCallback");
            }

            Preferences.Set(workloadName, false);
            return result;
        }

        #region Permission Check

        public Task EnsureGrantedPermission<TPermission>() where TPermission : Permissions.BasePermission, new()
        {
            return MainThread.IsMainThread
                ? InnerCheckGeolocationPermission<TPermission>()
                : MainThread.InvokeOnMainThreadAsync(InnerCheckGeolocationPermission<TPermission>);
        }

        private async Task InnerCheckGeolocationPermission<TPermission>()
            where TPermission : Permissions.BasePermission, new()
        {
            if (typeof(TPermission) == typeof(Permissions.LocationAlways))
            {
                #region Special case region for 'LocationAlways' permission request

                // Required to initially request the 'LocationWhenInUse' permission to get the 'LocationAlways' iOS query Dialog immediately.
                // Otherwise when omitted, the iOS OS shows the 'LocationAlways' query dialog anytime.
                // See SO comment on that post: https://stackoverflow.com/q/68893241
                PermissionStatus locWhenInUsePerm =
                    await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                Log($"Check  Permission '{nameof(Permissions.LocationWhenInUse)}' Status: {locWhenInUsePerm}");

                if (locWhenInUsePerm != PermissionStatus.Granted)
                {
                    locWhenInUsePerm = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    Log($"Requested Permission '{nameof(Permissions.LocationWhenInUse)}' Status: {locWhenInUsePerm}");
                }

                #endregion
            }

            PermissionStatus permissionStatus = await Permissions.CheckStatusAsync<TPermission>();
            Log($"Check  Permission '{typeof(TPermission).Name} Status: {permissionStatus}");

            if (permissionStatus == PermissionStatus.Granted)
            {
                return;
            }

            permissionStatus = await Permissions.RequestAsync<TPermission>();
            Log($"Requested Permission '{typeof(TPermission).Name}' Status: {permissionStatus}");


            if (permissionStatus == PermissionStatus.Granted)
            {
                return;
            }

            throw new PermissionException($"{typeof(TPermission).Name} permission was not granted: {permissionStatus}");
        }

        #endregion

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

            _loggerInstance.Log(OSLogLevel.Info, message);
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


        public void AquireCpuWakeLock(string logTag)
        {
        }

        public void ReleaseCpuWakeLock(string logTag)
        {
        }

        #endregion
    }
}
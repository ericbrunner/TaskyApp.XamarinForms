using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Legacy.Content;
using TaskyApp.Contracts;
using TaskyApp.Tasky;
using Xamarin.Forms;

namespace TaskyApp.Droid.Tasky
{
    [Service]
    public class TaskRunnerService : Service
    {
        public static readonly string LogTag = $"X:{nameof(TaskyApp)}-{nameof(TaskRunnerService)}";

        private ITaskRunner? _taskRunner;
        private CancellationTokenSource? _cts;

        // ReSharper disable once InconsistentNaming
        public const int SERVICE_ID = 10000;

        // ReSharper disable once InconsistentNaming
        private const string SERVICE_NOTIFICATION_CHANNEL_ID = "10001";

        public override void OnCreate()
        {
            base.OnCreate();

            _taskRunner = App.Get<ITaskRunner>();
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            try
            {
                if (_taskRunner == null)
                    throw new InvalidOperationException($"{nameof(TaskRunner)} is null.");

                // wakeup cpu on API LEVEL < API 26 (Android 8.0, Android O), Release in respective branch below.
                _taskRunner.AquireCpuWakeLock(LogTag);

                if (intent?.Action != null && intent.Action.Equals(ACTION_PAUSE_TIMER))
                {
                    _taskRunner?.Log("PAUSE Record pressed", LogTag);
                }
                else if (intent?.Action != null && intent.Action.Equals(ACTION_STOP_TIMER))
                {
                    _taskRunner?.Log("STOP Record pressed", LogTag);
                }
                else
                {
                    _cts = new CancellationTokenSource();

                    Notification notification = BuildServiceNotification();
                    StartForeground(SERVICE_ID, notification);

                    int interval = intent?.GetIntExtra("interval", defaultValue: 0) ?? 0;

                    _taskRunner?.Log($"Service Started with Interval: {interval} sec. and flag:{flags}", LogTag);

                    int setInterval = interval * 1000; // in msec.

                    if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                if (_taskRunner?.DoWorkFunc == null)
                                    throw new InvalidOperationException($"{nameof(TaskRunner.DoWorkFunc)} is null.");

                                if (setInterval == 0)
                                {
                                    await _taskRunner.DoWorkFunc.Invoke(LogTag, _cts.Token);
                                }
                                else
                                {
                                    while (true)
                                    {
                                        _cts.Token.ThrowIfCancellationRequested();

                                        await _taskRunner.DoWorkFunc.Invoke(LogTag, _cts.Token);

                                        await Task.Delay(setInterval, _cts.Token);
                                    }
                                }
                            }
                            catch (Android.OS.OperationCanceledException)
                            {
                                _taskRunner?.Log("Service Stopped gracefully (Cancellation issued).", LogTag);
                            }
                            catch (Exception e)
                            {
                                _taskRunner?.Log($"Task.Run Exception: {e.Message}", LogTag);
                            }
                        }, _cts.Token);
                    }
                    else
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                if (_taskRunner == null)
                                    throw new InvalidOperationException($"{nameof(TaskRunner)} is null.");


                                if (_taskRunner?.DoWorkFunc == null)
                                    throw new InvalidOperationException($"{nameof(TaskRunner.DoWorkFunc)} is null.");

                                await _taskRunner.DoWorkFunc.Invoke(LogTag, _cts.Token);
                            }
                            catch (Android.OS.OperationCanceledException)
                            {
                                _taskRunner?.Log("Service Stopped gracefully (Cancellation issued).", LogTag);
                            }
                            catch (Exception e)
                            {
                                _taskRunner?.Log($"Task.Run Exception: {e.Message}", LogTag);
                            }
                            finally
                            {
                                try
                                {
                                    // Re-register a broadcast to restart that service

                                    if (GetSystemService(AlarmService) is AlarmManager manager)
                                    {
                                        int minInterval = 60 * 1000;
                                       
                                        // Minimum itnerval to set a broadcast alarm is 60 seconds (60*1000) in msec.
                                        int usedInterval = setInterval < minInterval ? minInterval : setInterval;

                                        long triggerAtTime = SystemClock.ElapsedRealtime() + usedInterval;

                                        Intent broadcastIntent =
                                            new Intent(action: GpsConstants.TaskRunnerServiceRestart);

                                        broadcastIntent.PutExtra("interval", interval);

                                        PendingIntent? pendingintent =
                                            PendingIntent.GetBroadcast(this, 245, broadcastIntent, PendingIntentFlags.Immutable);


                                        manager.Cancel(pendingintent); // cancel any previous registered broadcasts
                                        manager.SetAndAllowWhileIdle(AlarmType.ElapsedRealtimeWakeup, triggerAtTime,
                                            pendingintent);
                                        _taskRunner?.Log("Alarm SetAndAllowWhileIdle Set", LogTag);
                                    }
                                }
                                catch (Exception e)
                                {
                                    _taskRunner?.Log($"Re-register {nameof(TaskRunnerService)} Exception: {e.Message}",
                                        LogTag);
                                }

                                try
                                {
                                    _taskRunner?.ReleaseCpuWakeLock(LogTag);
                                    // link: https://developer.android.com/training/scheduling/wakelock#wakeful
#pragma warning disable CS0618 - Not obsolete in older android platforms with API LEVEL < API 26 (Android 8.0)
                                    WakefulBroadcastReceiver.CompleteWakefulIntent(intent);
#pragma warning restore CS0618 - Not obsolete in older android platforms with API LEVEL < API 26 (Android 8.0)
                                }
                                catch (Exception e)
                                {
                                    _taskRunner?.Log(
                                        $"Re-register: CompleteWakefulIntent {nameof(TaskRunnerService)} Exception: {e.Message}",
                                        LogTag);
                                }
                            }
                        }, _cts.Token);
                    }
                }
            }
            catch (Exception e)
            {
                _taskRunner?.Log($"Exception: {e.Message}", LogTag);
            }
            finally
            {
                _taskRunner?.ReleaseCpuWakeLock(LogTag);
            }

            return StartCommandResult.Sticky;
        }

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        public override void OnDestroy()
        {
            _cts?.Cancel();

            _taskRunner?.Log("Service Stopped.", LogTag);

            base.OnDestroy();
        }

        #region Notification

        private Notification BuildServiceNotification()
        {
            var intent = new Intent(Android.App.Application.Context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.SingleTop);
            intent.PutExtra("Title", "Message");

            var pendingIntent = PendingIntent.GetActivity(Android.App.Application.Context, 0, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            NotificationCompat.Builder notificationBuilder =
                    new NotificationCompat.Builder(Android.App.Application.Context, SERVICE_NOTIFICATION_CHANNEL_ID)
                        .SetContentTitle($"{nameof(TaskyApp)} GPS Background Tracking")
                        .SetContentText("Your location is being tracked")
                        .SetSmallIcon(Resource.Drawable.location)
                        .SetTicker("TIME4-GPS")
                        .SetOngoing(true)
                        .SetContentIntent(pendingIntent)
                        .AddAction(BuildPauseAction())
                        .AddAction(BuildStopAction())
                ;

            if (global::Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannel notificationChannel =
                    new NotificationChannel(SERVICE_NOTIFICATION_CHANNEL_ID, "Title", NotificationImportance.High);
                notificationChannel.Importance = NotificationImportance.High;
                notificationChannel.EnableLights(true);
                notificationChannel.EnableVibration(true);
                notificationChannel.SetShowBadge(true);
                notificationChannel.SetVibrationPattern(new long[] { 100, 200, 300 });

                if (Android.App.Application.Context.GetSystemService(Android.Content.Context.NotificationService) is
                    NotificationManager notificationManager)
                {
                    notificationBuilder.SetChannelId(SERVICE_NOTIFICATION_CHANNEL_ID);
                    notificationManager.CreateNotificationChannel(notificationChannel);
                }
            }

            return notificationBuilder.Build();
        }

        // ReSharper disable once InconsistentNaming
        public static readonly string ACTION_PAUSE_TIMER = $"{nameof(TaskRunnerService)}.action.PAUSE_TIMER";
        public static readonly string ACTION_STOP_TIMER = $"{nameof(TaskRunnerService)}.action.STOP_TIMER";

        private NotificationCompat.Action? BuildStopAction()
        {
            var stopTimerIntent = new Intent(this, GetType());
            stopTimerIntent.SetAction(ACTION_STOP_TIMER);
            var stopTimerPendingIntent =
                PendingIntent.GetService(this, 0, stopTimerIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var builder = new NotificationCompat.Action.Builder(
                Resource.Drawable.ic_stop,
                "Stop",
                stopTimerPendingIntent);

            return builder.Build();
        }

        public NotificationCompat.Action? BuildPauseAction()
        {
            var pauseTimerIntent = new Intent(this, GetType());
            pauseTimerIntent.SetAction(ACTION_PAUSE_TIMER);
            var pauseTimerPendingIntent =
                PendingIntent.GetService(this, 0, pauseTimerIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var builder = new NotificationCompat.Action.Builder(
                Resource.Drawable.ic_pause_circle_outline,
                "Pause",
                pauseTimerPendingIntent);

            return builder.Build();
        }

        #endregion
    }
}
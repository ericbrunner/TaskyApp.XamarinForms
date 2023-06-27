using System;
using Android.App;
using Android.Content;
using TaskyApp.Contracts;
using TaskyApp.Tasky;
using Xamarin.Essentials;

namespace TaskyApp.Droid.Tasky
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = GpsConstants.TaskRunnerServiceBroadcastReceiver, Label = "RestartTaskRunnerService")]
    [IntentFilter(new []{GpsConstants.TaskRunnerServiceRestart})]
    //link: https://developer.android.com/training/scheduling/wakelock#wakeful
#pragma warning disable CS0618 // Type or member is obsolete - Not obsolete in older android platforms with API LEVEL < API 26 (Android 8.0)
    public class TaskRunnerServiceReceiver : AndroidX.Legacy.Content.WakefulBroadcastReceiver
#pragma warning restore CS0618 // Type or member is obsolete - Not obsolete in older android platforms with API LEVEL < API 26 (Android 8.0)
    {
        private readonly ITaskRunner? _taskRunner;

        public TaskRunnerServiceReceiver()
        {
            _taskRunner = App.Get<ITaskRunner>();
        }
        public override void OnReceive(Context? context, Intent? intent)
        {
            try
            {
                int interval = 0;
                if (intent != null)
                {
                    interval = intent.GetIntExtra("interval", defaultValue: 0);
                }
                
                var taskRunnerServiceIntent = new Intent(context ?? Platform.AppContext, typeof(TaskRunnerService));
                taskRunnerServiceIntent.PutExtra("interval", interval);
                
                StartWakefulService(context ?? Platform.AppContext, taskRunnerServiceIntent);

                _taskRunner?.Log($"Re-Start of {nameof(TaskRunnerService)} succeeded");
            }
            catch (Exception e)
            {
                _taskRunner?.Log($"Re-Start of {nameof(TaskRunnerService)} failed. Exception: {e.Message}");
            }
        }
    }
}
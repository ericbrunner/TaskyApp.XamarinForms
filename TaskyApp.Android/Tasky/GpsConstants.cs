namespace TaskyApp.Droid.Tasky
{
    public sealed class GpsConstants
    {
        public const string TaskRunnerServiceRestart = "com.companyname.taskyapp.GeoLocation.RestartTaskRunnerService";

        public const string TaskRunnerServiceBroadcastReceiver =
            "com.companyname.taskyapp.GeoLocation.TaskRunnerServiceBroadcastReceiver";

        public const string TaskRunnerServiceWakeLock = nameof(TaskRunnerServiceWakeLock);
    }
}
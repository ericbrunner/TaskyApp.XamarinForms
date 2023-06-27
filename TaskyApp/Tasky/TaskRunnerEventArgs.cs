using System;

namespace TaskyApp.Tasky
{
    public class TaskRunnerEventArgs: EventArgs{
        public Exception? Exception { get; }
        public BackgroundTaskStatus TaskStatus { get; }

        public TaskRunnerEventArgs(BackgroundTaskStatus taskStatus, Exception? exception)
        {
            TaskStatus = taskStatus;
            Exception = exception;
        }
    }
}
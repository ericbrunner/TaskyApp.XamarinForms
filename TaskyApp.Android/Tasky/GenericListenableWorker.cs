using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Runtime;
using AndroidX.Concurrent.Futures;
using AndroidX.Work;
using Google.Common.Util.Concurrent;
using Javax.Security.Auth;
using TaskyApp.Tasky.Messages;
using Xamarin.Forms;
using Object = Java.Lang.Object;

namespace TaskyApp.Droid.Tasky
{
    public class GenericListenableWorker : ListenableWorker, CallbackToFutureAdapter.IResolver
    {
        public const string Tag = nameof(GenericListenableWorker);
        public static WorkerBag WorkerBag { get; set; } = new();


        public GenericListenableWorker(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference,
            transfer)
        {
        }

        public GenericListenableWorker(Context appContext, WorkerParameters workerParams) : base(appContext,
            workerParams)
        {
        }

        //Signals that the work has started.
        public override IListenableFuture StartWork()
        {
            Debug.Write($"{DateTime.Now:O}-{nameof(GenericListenableWorker)}.{nameof(StartWork)} invoked");
            return CallbackToFutureAdapter.GetFuture(this);
        }

        public Object AttachCompleter(CallbackToFutureAdapter.Completer p0)
        {
            Debug.Write($"{DateTime.Now:O}-{nameof(GenericListenableWorker)}.{nameof(AttachCompleter)} invoked");

            var workerTask = Task.Run(async () =>
            {
                if (WorkerBag.BackgroundTask == null)
                {
                    p0.Set(Result.InvokeFailure(Data.Empty));
                    return;
                }

                try
                {
                    if (WorkerBag.Interval == null)
                    {
                        await WorkerBag.BackgroundTask(WorkerBag.CancellationToken);
                    }
                    else
                    {
                        while (true)
                        {
                            WorkerBag.CancellationToken.ThrowIfCancellationRequested();

                            await WorkerBag.BackgroundTask(WorkerBag.CancellationToken);

                            await Task.Delay(WorkerBag.Interval.Value, WorkerBag.CancellationToken);
                        }
                    }

                    p0.Set(Result.InvokeSuccess());
                }
                catch (OperationCanceledException oce)
                {
                    p0.Set(Result.InvokeSuccess());
                    throw;
                }
                catch (Exception ex)
                {
                    p0.Set(Result.InvokeFailure(Data.Empty));
                    throw;
                }
            });

            MessagingCenter.Send<GenericListenableWorkerMessage, Task>(new GenericListenableWorkerMessage(),
                "TaskCallback", workerTask);
            return Tag;
        }
    }
}
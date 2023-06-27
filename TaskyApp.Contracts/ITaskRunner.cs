using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace TaskyApp.Contracts;

public interface ITaskRunner
{
    bool IsPingPongServerEnabled { get; }
    Task RunTask(Func<CancellationToken, Task> backgroundTask,
        CancellationToken cancellationToken,
        TimeSpan? interval = null);


    void RunWorker(Func<CancellationToken, Task> backgroundTask,
        CancellationToken cancellationToken,
        TimeSpan? interval = null);


    Func<string, CancellationToken, Task>? DoWorkFunc { get; }

    Task StartService(Func<string, CancellationToken, Task> serviceFunc, string workloadName,
        TimeSpan? interval = null);

    Task<bool> StopService(string workloadName);
        
    Task EnsureGrantedPermission<TPermission>() where TPermission : Permissions.BasePermission, new();

    void Log(string message, string tag = nameof(ITaskRunner), Exception? exception = null,
        [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFileName = "");

    void AquireCpuWakeLock(string logTag);
    void ReleaseCpuWakeLock(string logTag);
}
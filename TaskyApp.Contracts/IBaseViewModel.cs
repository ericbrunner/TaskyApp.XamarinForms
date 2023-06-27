using System.ComponentModel;

namespace TaskyApp.Contracts;

public interface IBaseViewModel
{
    bool IsBusy { get; set; }
    string Title { get; set; }
    event PropertyChangedEventHandler? PropertyChanged;
}
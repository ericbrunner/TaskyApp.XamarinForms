using System;
using System.ComponentModel;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using static System.Diagnostics.Debug;


[assembly:ExportEffect(typeof(TaskyApp.iOS.Effects.FocusEffect), nameof(TaskyApp.iOS.Effects.FocusEffect))]
namespace TaskyApp.iOS.Effects;

public class FocusEffect : PlatformEffect
{
    private UIColor backgroundColor;

    protected override void OnAttached()
    {
        WriteLine($"EFFECT: {nameof(FocusEffect)}.{nameof(OnAttached)} invoked.");

        try
        {
            backgroundColor = UIColor.FromRGB(210, 153, 245);
            Control.BackgroundColor = backgroundColor;
        }
        catch (Exception e)
        {
            WriteLine($"Can't set property on attached control. Error: {e.Message}");
        }
    }

    protected override void OnDetached()
    {
        WriteLine($"EFFECT: {nameof(FocusEffect)}.{nameof(OnDetached)} invoked.");
    }

    protected override void OnElementPropertyChanged(PropertyChangedEventArgs args)
    {
        base.OnElementPropertyChanged(args);

        try
        {
            if (!args.PropertyName.Equals(nameof(Entry.IsFocused))) return;

            Control.BackgroundColor = Control.BackgroundColor == backgroundColor ? UIColor.White : backgroundColor;
        }
        catch (Exception e)
        {
            WriteLine($"Can't set property on attached control. Error: {e.Message}");
        }
    }
}
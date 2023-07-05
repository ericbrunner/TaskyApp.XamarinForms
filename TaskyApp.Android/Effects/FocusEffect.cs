using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using static System.Diagnostics.Debug;

[assembly: ExportEffect(typeof(TaskyApp.Droid.Effects.FocusEffect), nameof(TaskyApp.Droid.Effects.FocusEffect))]

namespace TaskyApp.Droid.Effects;

public class FocusEffect : PlatformEffect
{
    private Android.Graphics.Color originalBackgroundColor = new Android.Graphics.Color(0, 0, 0, 0);
    private Android.Graphics.Color backgroundColor;

    protected override void OnAttached()
    {
        WriteLine($"EFFECT: {nameof(FocusEffect)}.{nameof(OnAttached)} invoked.");

        try
        {
            backgroundColor = Android.Graphics.Color.LightGreen;
            Control.SetBackgroundColor(backgroundColor);
        }
        catch (Exception e)
        {
            WriteLine($"EFFECT: Can't set property on attached control. Error: {e.Message}");
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

            var currentColor = (Control.Background as Android.Graphics.Drawables.ColorDrawable)?.Color;

            if (currentColor == null) return;

            Control.SetBackgroundColor(currentColor == backgroundColor ? originalBackgroundColor : backgroundColor);
        }
        catch (Exception e)
        {
            WriteLine($"Can't set property on attached control. Error: {e.Message}");
        }
    }
}
using TaskyApp.CustomControls;
using TaskyApp.Droid.CustomRenderer;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly:ExportRenderer(typeof(MyEntry), typeof(MyEntryRenderer))]
namespace TaskyApp.Droid.CustomRenderer;

public class MyEntryRenderer : EntryRenderer
{
    protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
    {
        base.OnElementChanged(e);

        if (Control == null) return;

        Control.SetBackgroundColor(global::Android.Graphics.Color.LightGreen);
    }
}
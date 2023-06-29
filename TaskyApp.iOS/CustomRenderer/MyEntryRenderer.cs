using TaskyApp.CustomControls;
using TaskyApp.iOS.CustomRenderer;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(MyEntry), typeof(MyEntryRenderer))]
namespace TaskyApp.iOS.CustomRenderer;

public class MyEntryRenderer : EntryRenderer
{
    protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
    {
        base.OnElementChanged(e);

        if (Control == null) return;
        
        Control.TextColor = UIColor.White;
        Control.BackgroundColor = UIColor.Blue;
        Control.BorderStyle = UITextBorderStyle.RoundedRect;
    }
}
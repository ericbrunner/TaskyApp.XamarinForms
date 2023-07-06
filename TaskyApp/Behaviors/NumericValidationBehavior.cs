using Xamarin.Forms;

namespace TaskyApp.Behaviors;

public class NumericValidationBehavior : Behavior<Entry>
{
    protected override void OnAttachedTo(Entry entry)
    {
        entry.TextChanged += EntryOnTextChanged;
        base.OnAttachedTo(entry);
    }

    private void EntryOnTextChanged(object sender, TextChangedEventArgs e)
    {
        var isValid = double.TryParse(e.NewTextValue, out _);
        ((Entry)sender).TextColor = isValid ? Color.Green : Color.Red;
    }
}
using Xamarin.Forms;

namespace TaskyApp.Effects;

public class FocusEffect : RoutingEffect
{
    public const string ResolutionGroupName = "com.companyname.taskyapp";

    public FocusEffect() : base($"{ResolutionGroupName}.{nameof(FocusEffect)}")
    {
    }
}
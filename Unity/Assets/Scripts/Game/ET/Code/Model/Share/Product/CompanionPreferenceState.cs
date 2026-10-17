#if !DOTNET
namespace ET
{
    public enum CompanionWorkPreference { Automatic = 0, AskFirst = 1, WashFirst = 2 }

    [ComponentOf(typeof(RestaurantRuntime))]
    public sealed class CompanionPreferenceState : Entity, IAwake
    {
        public CompanionWorkPreference Value;
    }
}
#endif

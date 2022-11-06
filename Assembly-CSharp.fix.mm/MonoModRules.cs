using MonoMod.InlineRT;

namespace MonoMod;

static class MonoModRules {
    public const string FlagGog = "GOG";
    public const string FlagSteam = "Steam";
    public const string FlagDLC = "DLC";

    static MonoModRules() {
        MonoModRule.Flag.Set(FlagGog, MonoModRule.Modder.FindType("OnlineInterfaceGog") != null);
        MonoModRule.Flag.Set(FlagSteam, MonoModRule.Modder.FindType("OnlineInterfaceSteam") != null);
        MonoModRule.Flag.Set(FlagDLC, MonoModRule.Modder.FindType("DLCManager") != null);
    }
}
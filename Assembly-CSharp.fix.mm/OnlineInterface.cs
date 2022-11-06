using System.IO;
using MonoMod;
using MonoMod.InlineRT;
using UnityEngine;

namespace AssemblyCSharp.fix.mm {
    [MonoModIfFlag("GOG")]
    [MonoModPatch("global::OnlineInterfaceGog")]
    public class OnlineInterfaceGog {
        private string SavePath {
            [MonoModReplace] get => Directory.GetParent(Application.dataPath).FullName;
        }
    }

    [MonoModIfFlag("Steam")]
    [MonoModPatch("global::OnlineInterfaceSteam")]
    public class OnlineInterfaceSteam {
        private string SavePath {
            [MonoModReplace] get => Directory.GetParent(Application.dataPath).FullName;
        }
    }
}

namespace MonoMod {
    static partial class MonoModRules {
        static MonoModRules() {
            MonoModRule.Flag.Set("GOG", MonoModRule.Modder.FindType("OnlineInterfaceGog") != null);
            MonoModRule.Flag.Set("Steam", MonoModRule.Modder.FindType("OnlineInterfaceSteam") != null);
        }
    }
}
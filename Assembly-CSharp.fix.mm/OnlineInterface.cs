using System.IO;
using MonoMod;
using UnityEngine;

namespace AssemblyCSharp.fix.mm; 

[MonoModIfFlag(MonoModRules.FlagGog)]
[MonoModPatch("global::OnlineInterfaceGog")]
public class OnlineInterfaceGog {
    private string SavePath {
        [MonoModReplace] get => Directory.GetParent(Application.dataPath).FullName;
    }
}

[MonoModIfFlag(key: MonoModRules.FlagSteam)]
[MonoModPatch("global::OnlineInterfaceSteam")]
public class OnlineInterfaceSteam {
    private string SavePath {
        [MonoModReplace] get => Directory.GetParent(Application.dataPath).FullName;
    }
}
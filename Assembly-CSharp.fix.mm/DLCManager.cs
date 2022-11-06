using System.IO;
using MonoMod;
using UnityEngine;

namespace AssemblyCSharp.fix.mm;

[MonoModPatch("global::DLCManager")]
public class DLCManager {
    [MonoModReplace]
    public static bool DLCEnabled() {
        return File.Exists(string.Join(Path.DirectorySeparatorChar.ToString(), new [] {
            Application.dataPath,
            "StreamingAssets",
            "AssetBundles",
            "atlas_achievements_dlc"
        }));
    }
}
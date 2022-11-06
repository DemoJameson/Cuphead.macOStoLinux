using System;
using System.IO;
using System.Reflection;

namespace Cuphead.macOStoLinux;

public class Program {
    private static string currentDir;
    private static string pathCupheadApp;
    private static string pathCupheadAppContents;
    private static string pathLog;
    private static string linuxBuildLibsUnityDir;
    private static string linuxBuildLibsCommonDir;
    private static string targetDir;
    private static string targetDataDir;

    private static Assembly asmMonoMod;

    private static void LogLine(string line) {
        Console.WriteLine(line);
    }

    private static void LogErr(string line) {
        Console.Error.WriteLine(line);
    }

    private static int Main(string[] args) {
        Console.WriteLine("Cuphead.macOStoLinux");

        if (!SetupPaths()) {
            return 1;
        }

        if (File.Exists(pathLog))
            File.Delete(pathLog);
        using Stream fileStream = File.OpenWrite(pathLog);
        using StreamWriter fileWriter = new StreamWriter(fileStream, Console.OutputEncoding);
        using LogWriter logWriter = new LogWriter {
            STDOUT = Console.Out,
            File = fileWriter
        };

        Console.SetOut(logWriter);

        try {
            if (!IsMonoVersionCompatible()) {
                LogErr("Cuphead.macOStoLinux only works with Mono 5 and higher.");
                LogErr("Please upgrade Mono and run the installer again.");
                throw new Exception("Incompatible Mono version");
            }

            MoveMacGameFiles();
            CopyLinuxBuildLibs();
            DeleteMacLibs();

            if (asmMonoMod == null) {
                LoadMonoMod();
            }

            string assemblyCsharpDll = PathCombine(targetDataDir, "Managed", "Assembly-CSharp.dll");
            RunMonoMod(assemblyCsharpDll, assemblyCsharpDll,
                new[] {PathCombine(currentDir, "Assembly-CSharp.fix.mm.dll")});
        } catch (Exception e) {
            string msg = e.ToString();
            LogLine("");
            LogErr(msg);
            LogErr("");
            LogErr("Converting Cuphead macOS build to Linux build failed.");

            if (msg.Contains("--->")) {
                LogErr("Please review the error after the '--->' to see if you can fix it on your end.");
            }

            LogErr("");
            LogErr("If you need help, please create a new issue on GitHub @ https://github.com/DemoJameson/Cuphead.macOStoLinux");
            LogErr("Make sure to upload your log file.");

            return 1;
        } finally {
            // Let's not pollute <insert installer name here>.
            Environment.SetEnvironmentVariable("MONOMOD_DEPDIRS", "");
            Environment.SetEnvironmentVariable("MONOMOD_MODS", "");
            Environment.SetEnvironmentVariable("MONOMOD_DEPENDENCY_MISSING_THROW", "");
        }

        Console.SetOut(logWriter.STDOUT);
        logWriter.STDOUT = null;

        return 0;
    }

    private static bool IsMonoVersionCompatible() {
        // Outdated Mono versions can corrupt Celeste.exe when patching.
        // (see https://github.com/EverestAPI/Everest/issues/62)

        try {
            // Mono version detection code: https://stackoverflow.com/a/4180030
            Type monoRuntimeType = Type.GetType("Mono.Runtime");
            if (monoRuntimeType != null) {
                MethodInfo getDisplayNameMethod = monoRuntimeType.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (getDisplayNameMethod != null) {
                    string version = (string) getDisplayNameMethod.Invoke(null, null);
                    // version should look like this: "6.8.0.123 (tarball Tue May 12 15:13:37 UTC 2020)"
                    int majorVersion = int.Parse(version.Split('.')[0]);

                    // major 5 should work while people have issues with major 4
                    if (majorVersion < 5) {
                        return false;
                    }
                }
            }

            // if the runtime isn't Mono or if Mono isn't too old, it should be compatible
            return true;
        } catch (Exception) {
            // ignore exception and continue, we don't want to block users if "GetDisplayName" changes
            LogLine("Could not determine Mono version.");
            LogLine("Cuphead.macOStoLinux works with Mono 5 and higher.");
            LogLine("Please see https://github.com/EverestAPI/Everest/issues/62 if you run into issues.");
            return true;
        }
    }

    private static bool SetupPaths() {
        currentDir = Directory.GetCurrentDirectory();
        Console.WriteLine(currentDir);

        pathCupheadApp = Path.Combine(currentDir, "Cuphead.app");
        if (!Directory.Exists(pathCupheadApp)) {
            LogErr("Cuphead.app not found!");
            LogErr("Did you extract the .zip into the same place as Cuphead.app?");
            return false;
        }

        pathCupheadAppContents = Path.Combine(pathCupheadApp, "Contents");

        string infoPlist = PathCombine(pathCupheadAppContents, "Info.plist");
        if (!File.Exists(infoPlist)) {
            LogErr($"{infoPlist} not found!");
            LogErr("Did you download the complete Cuphead.app?");
            return false;
        }

        string unityVersion;
        string infoPlistContent = File.ReadAllText(infoPlist);
        if (infoPlistContent.Contains("version 5.6.2")) {
            unityVersion = "5.6.2";
        } else if (infoPlistContent.Contains("version 2017.4.9")) {
            unityVersion = "2017.4.9";
        } else {
            LogErr("Incompatible version of Unity");
            return false;
        }

        linuxBuildLibsUnityDir = PathCombine(currentDir, "Linux_Build_Libs", "Cuphead_Linux_Unity_" + unityVersion);
        if (!Directory.Exists(linuxBuildLibsUnityDir)) {
            LogErr($"{linuxBuildLibsUnityDir} not found!");
            return false;
        }
        
        linuxBuildLibsCommonDir = PathCombine(currentDir, "Linux_Build_Libs", "Cuphead_Linux_Common");
        if (!Directory.Exists(linuxBuildLibsCommonDir)) {
            LogErr($"{linuxBuildLibsCommonDir} not found!");
            return false;
        }

        pathLog = Path.Combine(currentDir, "log.txt");

        targetDir = Path.Combine(currentDir, "Cuphead_Linux");
        targetDataDir = Path.Combine(targetDir, "Cuphead_Data");
        if (!Directory.Exists(targetDir)) {
            LogLine("Creating Cuphead_Linux directory");
            Directory.CreateDirectory(targetDir);
        }

        return true;
    }

    private static void MoveMacGameFiles() {
        string dataDir = PathCombine(pathCupheadAppContents, "Resources", "Data");
        LogLine($"Copy files from {dataDir} to {targetDataDir}");
        Copy(dataDir, targetDataDir);

        LogLine($"Copy 'unity default resources'");
        string defaultResource = PathCombine(targetDataDir, "Resources", "unity default resources");
        if (File.Exists(defaultResource)) {
            File.Delete(defaultResource);
        }

        File.Copy(
            PathCombine(pathCupheadAppContents, "Resources", "unity default resources"),
            defaultResource
        );

        string monoEtc = PathCombine(pathCupheadAppContents, "Data", "Managed", "etc");
        if (!Directory.Exists(monoEtc)) {
            monoEtc = PathCombine(pathCupheadAppContents, "Mono", "etc");
        }
        LogLine($"Copy {monoEtc}");
        Copy(monoEtc, PathCombine(targetDataDir, "Mono", "etc"));
    }

    private static void CopyLinuxBuildLibs() {
        LogLine("Copying files from linux build libs");
        Copy(linuxBuildLibsUnityDir, targetDir);
        Copy(linuxBuildLibsCommonDir, targetDir);
    }

    private static void DeleteMacLibs() {
        string rewiredOSXLib = PathCombine(targetDataDir, "Managed", "Rewired_OSX_Lib.dll");
        LogLine($"Deleting {rewiredOSXLib}");
        if (File.Exists(rewiredOSXLib)) {
            File.Delete(rewiredOSXLib);
        }
    }

    private static void LoadMonoMod() {
        // We can't add MonoMod as a reference to MiniInstaller, as we don't want to accidentally lock the file.
        // Instead, load it dynamically and invoke the entry point.
        // We also need to lazily load any dependencies.
        LogLine("Loading Mono.Cecil");
        LazyLoadAssembly(Path.Combine(currentDir, "Mono.Cecil.dll"));
        LogLine("Loading Mono.Cecil.Mdb");
        LazyLoadAssembly(Path.Combine(currentDir, "Mono.Cecil.Mdb.dll"));
        LogLine("Loading Mono.Cecil.Pdb");
        LazyLoadAssembly(Path.Combine(currentDir, "Mono.Cecil.Pdb.dll"));
        LogLine("Loading MonoMod.Utils.dll");
        LazyLoadAssembly(Path.Combine(currentDir, "MonoMod.Utils.dll"));
        LogLine("Loading MonoMod");
        asmMonoMod = LazyLoadAssembly(Path.Combine(currentDir, "MonoMod.exe"));
    }

    private static void RunMonoMod(string asmFrom, string asmTo = null, string[] dllPaths = null) {
        asmTo ??= asmFrom;
        dllPaths ??= new[] {currentDir};

        LogLine($"Running MonoMod for {asmFrom}");
        // We're lazy.
        Environment.SetEnvironmentVariable("MONOMOD_DEPDIRS", currentDir);
        Environment.SetEnvironmentVariable("MONOMOD_MODS", string.Join(Path.PathSeparator.ToString(), dllPaths));
        Environment.SetEnvironmentVariable("MONOMOD_DEPENDENCY_MISSING_THROW", "0");
        int returnCode = (int) asmMonoMod.EntryPoint.Invoke(null, new object[] {new string[] {asmFrom, asmTo + ".tmp"}});

        if (returnCode != 0 && File.Exists(asmTo + ".tmp"))
            File.Delete(asmTo + ".tmp");

        if (!File.Exists(asmTo + ".tmp"))
            throw new Exception("MonoMod failed creating a patched assembly!");

        if (File.Exists(asmTo)) {
            File.Delete(asmTo);
        }

        File.Delete(asmTo + ".pdb");
        File.Move(asmTo + ".tmp", asmTo);
    }

    private static Assembly LazyLoadAssembly(string path) {
        LogLine($"Lazily loading {path}");
        ResolveEventHandler tmpResolver = (s, e) => {
            string asmPath = Path.Combine(Path.GetDirectoryName(path), new AssemblyName(e.Name).Name + ".dll");
            if (!File.Exists(asmPath))
                return null;
            return Assembly.LoadFrom(asmPath);
        };
        AppDomain.CurrentDomain.AssemblyResolve += tmpResolver;
        Assembly asm = Assembly.Load(Path.GetFileNameWithoutExtension(path));
        AppDomain.CurrentDomain.AssemblyResolve -= tmpResolver;
        AppDomain.CurrentDomain.TypeResolve += (s, e) => { return asm.GetType(e.Name) != null ? asm : null; };
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => { return e.Name == asm.FullName || e.Name == asm.GetName().Name ? asm : null; };
        return asm;
    }

    private static void Copy(string sourceDirectory, string targetDirectory) {
        DirectoryInfo diSource = new(sourceDirectory);
        DirectoryInfo diTarget = new(targetDirectory);

        CopyAll(diSource, diTarget);
    }

    private static void CopyAll(DirectoryInfo source, DirectoryInfo target) {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles()) {
            LogLine($"Copying {fi.FullName} +> {Path.Combine(target.FullName, fi.Name)}");
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories()) {
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }

    private static string PathCombine(params string[] paths) {
        if (paths.Length < 2) {
            throw new ArgumentException("At lease two paths");
        }

        string result = paths[0];
        for (var i = 1; i < paths.Length; i++) {
            result = Path.Combine(result, paths[i]);
        }

        return result;
    }
}
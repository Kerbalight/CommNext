using CommNext.Models;
using Unity.Collections;

namespace CommNext.Patches;

public class CommNetManagerPatches
{
    public static NativeArray<CommNextBodyInfo> bodyInfos;
    
    public static void InitializeBodies()
    {
        // Harmony harmony = new Harmony("CommNext");
        // harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}
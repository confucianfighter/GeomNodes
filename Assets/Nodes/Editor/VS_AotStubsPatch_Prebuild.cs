#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class VS_AotStubsPatch_Prebuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    // The file Visual Scripting generates (path from your logs)
    private const string AotStubsPath =
        "Assets/Unity.VisualScripting.Generated/VisualScripting.Core/AotStubs.cs";

    // Members that caused your CS1061 errors in Android builds
    private static readonly string[] BadTokens =
    {
        "scaleInLightmap",
        "receiveGI",
        "stitchLightmapSeams",
        ".parent",
        "isVariant"
    };

    public void OnPreprocessBuild(BuildReport report)
    {
        if (!File.Exists(AotStubsPath))
        {
            Debug.Log($"[VS AOT Patch] No AotStubs.cs found at: {AotStubsPath}");
            return;
        }

        var original = File.ReadAllText(AotStubsPath);

        // Remove any lines that reference the bad tokens.
        // (This is intentionally conservative: line-based removal.)
        var sb = new StringBuilder(original.Length);
        using (var reader = new StringReader(original))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                bool skip = false;
                for (int i = 0; i < BadTokens.Length; i++)
                {
                    if (line.Contains(BadTokens[i]))
                    {
                        skip = true;
                        break;
                    }
                }

                if (!skip)
                    sb.AppendLine(line);
            }
        }

        var patched = sb.ToString();
        if (patched != original)
        {
            File.WriteAllText(AotStubsPath, patched);
            Debug.Log("[VS AOT Patch] Patched AotStubs.cs (removed unsupported members).");
        }
        else
        {
            Debug.Log("[VS AOT Patch] AotStubs.cs already clean (no changes).");
        }
    }
}
#endif

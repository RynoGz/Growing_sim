using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Growveld.Editor
{
    /// <summary>
    /// Runs an explicitly requested one-shot editor setup after scripts compile.
    /// The pending marker is deleted before invocation so domain reloads are safe.
    /// </summary>
    [InitializeOnLoad]
    public static class PendingPhaseSetupRunner
    {
        // Kept in source so adding a pending setup can deliberately trigger a fresh editor reload.
        private const string PendingSetupPath = "Assets/_Project/Editor/PendingPhaseSetup.txt";

        static PendingPhaseSetupRunner()
        {
            EditorApplication.delayCall += RunPendingSetup;
        }

        internal static void RunPendingSetup()
        {
            string absolutePath = Path.GetFullPath(PendingSetupPath);
            if (!File.Exists(absolutePath))
            {
                return;
            }

            string methodIdentifier = File.ReadAllText(absolutePath).Trim();

            int separatorIndex = methodIdentifier.LastIndexOf('.');
            if (separatorIndex <= 0 || separatorIndex >= methodIdentifier.Length - 1)
            {
                Debug.LogError($"Invalid pending setup method: {methodIdentifier}");
                return;
            }

            string typeName = methodIdentifier.Substring(0, separatorIndex);
            string methodName = methodIdentifier.Substring(separatorIndex + 1);
            Type targetType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(type => type != null);
            MethodInfo method = targetType?.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);

            if (method == null)
            {
                Debug.Log($"Pending setup method is not compiled yet; Unity will retry after reload: {methodIdentifier}");
                return;
            }

            AssetDatabase.DeleteAsset(PendingSetupPath);

            try
            {
                method.Invoke(null, null);
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogException(exception.InnerException ?? exception);
            }
        }
    }

    public sealed class PendingPhaseSetupAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets.Contains("Assets/_Project/Editor/PendingPhaseSetup.txt"))
            {
                EditorApplication.delayCall += PendingPhaseSetupRunner.RunPendingSetup;
            }
        }
    }
}

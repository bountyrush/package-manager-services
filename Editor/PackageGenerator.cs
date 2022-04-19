using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

namespace BountyRush.PackageManagerServices
{
    public class PackageGenerator
    {
        #region Static fields

        private     static  Dictionary<PackageGeneratorOptions, string>     s_folderMap                 = new Dictionary<PackageGeneratorOptions, string>()
        {
            { PackageGeneratorOptions.IncludeRuntime, "Runtime" },
            { PackageGeneratorOptions.IncludeRuntimeTests, "Tests/Runtime" },
            { PackageGeneratorOptions.IncludeEditor, "Editor" },
            { PackageGeneratorOptions.IncludeEditorTests, "Tests/Editor" },
            { PackageGeneratorOptions.IncludeResources, "Resources" },
            { PackageGeneratorOptions.IncludeEditorResources, "EditorResources" },
            { PackageGeneratorOptions.IncludeDocumentation, "Documentation" },
        };

        #endregion

        #region Static methods

        public static void Generate(string path, PackageDefinition package,
            PackageGeneratorOptions options, string assemblyName)
        {
            string  packagePath     = $"{path}/{package.Name}";
            if (AssetDatabase.IsValidFolder(packagePath))
            {
                var     canReplace  = EditorUtility.DisplayDialog("Replace Package?", "Are you sure you want to replace existing package", "Ok", "Cancel");
                if (!canReplace) return;

                AssetDatabase.DeleteAsset(packagePath);
            }

            // create default assets
            AssetDatabase.CreateFolder(path, package.Name);
            SerializeObjectAsTextAsset(path: $"{packagePath}/package.json", obj: package);

            // create required folders
            foreach (var item in s_folderMap)
            {
                if (HasValue(options: options, value: item.Key))
                {
                    CreateFolders(mainFolder: packagePath, subFolders: item.Value.Split(';'));
                }
            }

            // create assembly definition files
            if (HasValue(options: options, value: PackageGeneratorOptions.IncludeRuntime))
            {
                var     runtimeDefinition   = new AssemblyDefinition(
                    name: assemblyName,
                    overrideReferences: true,
                    autoReferenced: true);
                SerializeObjectAsTextAsset(
                    path: $"{packagePath}/Runtime/{assemblyName}.asmdef",
                    obj: runtimeDefinition);

                if (HasValue(options: options, value: PackageGeneratorOptions.IncludeRuntimeTests))
                {
                    var     testDefinition  = new AssemblyDefinition(
                        name: $"{assemblyName}.Tests",
                        optionalUnityReferences: new string[] { "TestAssemblies" },
                        includePlatforms: new string[] { "Editor" },
                        overrideReferences: true,
                        precompiledReferences: new string[] { "NSubstitute.dll" },
                        autoReferenced: true);
                    SerializeObjectAsTextAsset(
                        path: $"{packagePath}/Tests/Runtime/{assemblyName}.Tests.asmdef",
                        obj: testDefinition);
                }
            }
            if (HasValue(options: options, value: PackageGeneratorOptions.IncludeEditor))
            {
                var     editorDefinition    = new AssemblyDefinition(
                    name: $"{assemblyName}.Editor",
                    includePlatforms: new string[] { "Editor" },
                    overrideReferences: true,
                    autoReferenced: true);
                SerializeObjectAsTextAsset(
                    path: $"{packagePath}/Editor/{assemblyName}.Editor.asmdef",
                    obj: editorDefinition);

                if (HasValue(options: options, value: PackageGeneratorOptions.IncludeEditorTests))
                {
                    var     testDefinition  = new AssemblyDefinition(
                        name: $"{assemblyName}.Editor.Tests",
                        optionalUnityReferences: new string[] { "TestAssemblies" },
                        includePlatforms: new string[] { "Editor" },
                        overrideReferences: true,
                        precompiledReferences: new string[] { "NSubstitute.dll" },
                        autoReferenced: true);
                    SerializeObjectAsTextAsset(
                        path: $"{packagePath}/Tests/Editor/{assemblyName}.Editor.Tests.asmdef",
                        obj: testDefinition);
                }
            }
        }

        private static void CreateFolders(string mainFolder, string[] subFolders)
        {
            foreach (var subFolder in subFolders)
            {
                var     pathComponents  = subFolder.Split('/');
                var     currentPath     = mainFolder;
                foreach (var item in pathComponents)
                {
                    var     newPath     = $"{currentPath}/{item}";
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, item);
                    }
                    currentPath         = newPath;
                }
            }
        }

        private static string SerializeObjectAsTextAsset(string path, object obj)
        {
            var     jsonText    = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(path, jsonText);
            AssetDatabase.Refresh();

            return AssetDatabase.AssetPathToGUID(path);
        }

        private static bool HasValue(PackageGeneratorOptions options, PackageGeneratorOptions value)
        {
            return (options & value) != 0;
        }

        #endregion
    }
}
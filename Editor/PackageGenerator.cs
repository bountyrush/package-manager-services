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

            CreateAssemblyDefinitionFiles(packagePath, assemblyName, options);
        }

        private static void CreateAssemblyDefinitionFiles(string packagePath, string assemblyName, PackageGeneratorOptions options)
        {
            // runtime definitions
            var     runtimeAssemblyDefMeta      = default(StringKeyValuePair);
            if (HasValue(options: options, value: PackageGeneratorOptions.IncludeRuntime))
            {
                var     runtimeAssemblyName     = assemblyName;
                var     runtimeAssemblyDef      = new AssemblyDefinition(
                    name: runtimeAssemblyName,
                    overrideReferences: true,
                    autoReferenced: true);
                var     runtimeAssemblyDefGuid  = SerializeObjectAsTextAsset(
                    path: $"{packagePath}/Runtime/{runtimeAssemblyName}.asmdef",
                    obj: runtimeAssemblyDef);
                runtimeAssemblyDefMeta          = new StringKeyValuePair(runtimeAssemblyName, runtimeAssemblyDefGuid);

                if (HasValue(options: options, value: PackageGeneratorOptions.IncludeRuntimeTests))
                {
                    var     testAssemblyName    = $"{runtimeAssemblyName}.Tests";
                    var     testAssemblyDef     = new AssemblyDefinition(
                        name: testAssemblyName,
                        references: new string[] { GetAssemblyReferenceNameOrGuid(runtimeAssemblyDefMeta) },
                        optionalUnityReferences: new string[] { "TestAssemblies" },
                        includePlatforms: new string[] { "Editor" },
                        overrideReferences: true,
                        precompiledReferences: new string[] { "NSubstitute.dll" },
                        autoReferenced: true);
                    SerializeObjectAsTextAsset(
                        path: $"{packagePath}/Tests/Runtime/{testAssemblyName}.asmdef",
                        obj: testAssemblyDef);
                }
            }

            // editor definitions
            if (HasValue(options: options, value: PackageGeneratorOptions.IncludeEditor))
            {
                var     editorDependencies      = new List<StringKeyValuePair>();
                if (runtimeAssemblyDefMeta != null)
                {
                    editorDependencies.Add(runtimeAssemblyDefMeta);
                }

                var     editorAssemblyName      = $"{assemblyName}.Editor";
                var     editorAssemblyDef       = new AssemblyDefinition(
                    name: editorAssemblyName,
                    references: editorDependencies.ConvertAll((item) => GetAssemblyReferenceNameOrGuid(item)).ToArray(),
                    includePlatforms: new string[] { "Editor" },
                    overrideReferences: true,
                    autoReferenced: true);
                var     editorAssemblyDefGuid   = SerializeObjectAsTextAsset(
                    path: $"{packagePath}/Editor/{editorAssemblyName}.asmdef",
                    obj: editorAssemblyDef);
                var     editorAssemblyDefMeta   = new StringKeyValuePair(editorAssemblyName, editorAssemblyDefGuid);

                if (HasValue(options: options, value: PackageGeneratorOptions.IncludeEditorTests))
                {
                    var     testDependencies    = new List<StringKeyValuePair>(editorDependencies);
                    testDependencies.Add(editorAssemblyDefMeta);

                    var     testAssemblyName    = $"{editorAssemblyName}.Tests";
                    var     testAssemblyDef     = new AssemblyDefinition(
                        name: testAssemblyName,
                        references: testDependencies.ConvertAll((item) => GetAssemblyReferenceNameOrGuid(item)).ToArray(),
                        optionalUnityReferences: new string[] { "TestAssemblies" },
                        includePlatforms: new string[] { "Editor" },
                        overrideReferences: true,
                        precompiledReferences: new string[] { "NSubstitute.dll" },
                        autoReferenced: true);
                    SerializeObjectAsTextAsset(
                        path: $"{packagePath}/Tests/Editor/{testAssemblyName}.asmdef",
                        obj: testAssemblyDef);
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

        private static string GetAssemblyReferenceNameOrGuid(StringKeyValuePair meta)
        {
#if UNITY_2019_1_OR_NEWER
            return meta.Value; // guid
#else
            return meta.Key; // definition name
#endif
        }

        #endregion
    }
}
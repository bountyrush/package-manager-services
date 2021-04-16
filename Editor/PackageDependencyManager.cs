using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using Newtonsoft.Json;
using UnityEditor.PackageManager.Requests;

namespace BountyRush.PackageManagerServices
{
    [InitializeOnLoad]
    public class PackageDependencyManager : AssetPostprocessor
    {
        #region Constants

        private     const   string  kPackageManifestFileName            = "package.json";

        private     const   string  kProjectManifestFilePath            = "Packages/Manifest.json";

        private     const   string  kStateAutoResolvePackages           = "auto-resolve-state";

        #endregion

        #region Static fields

        private     static  ListRequest     s_getPackagesRequest;

        private     static  bool            s_isBusy;

        #endregion

        #region Constructors

        static PackageDependencyManager()
        {
            var     autoResolveState    = GetAutoResolvePackagesState();
            if (autoResolveState != OperationState.Done)
            {
                if (autoResolveState == OperationState.Pending)
                {
                    SetAutoResolvePackagesState(OperationState.Inprogress);
                }
                EditorApplication.delayCall += AutoResolvePackages;
            }
        }

        #endregion

        #region Private static methods

        private static OperationState GetAutoResolvePackagesState()
        {
            return (OperationState)SessionState.GetInt(kStateAutoResolvePackages, defaultValue: (int)OperationState.Pending);
        }

        private static void SetAutoResolvePackagesState(OperationState value)
        {
            SessionState.SetInt(kStateAutoResolvePackages, (int)value);

            if (value == OperationState.Inprogress)
            {
                Debug.Log("[PackageDependencyManager] Started asset package state check.");
            }
            else if (value == OperationState.Done)
            {
                Debug.Log("[PackageDependencyManager] Completed asset package state check.");
            }
        }

        private static void AutoResolvePackages()
        {
            // ensure that asset packages are installed in packages folder
            if (MoveAllAssetPackages())
            {
                return;
            }

            // find all the custom scoped registries used by the installed packages and add it to project manifest
            ResolveRegistries();
            SetAutoResolvePackagesState(OperationState.Done);
        }

        private static string[] FindPackages(string[] assets)
        {
            var     targetAssets    = new List<string>();
            foreach (var item in assets)
            {
                if (item.EndsWith(kPackageManifestFileName))
                {
                    var     parentFolderName    = Path.GetDirectoryName(item);
                    targetAssets.Add(parentFolderName);
                }
            }
            return targetAssets.ToArray();
        }

        private static void SetIsBusy(bool value)
        {
            // assign new value
            s_isBusy    = value;

            // update editor state
            if (s_isBusy)
            {
                EditorApplication.LockReloadAssemblies();
            }
            else
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        private static void SetProjectDirty()
        {
            SessionState.EraseInt(kStateAutoResolvePackages);
        }

        #endregion

        #region Manage manifest methods

        private static string GetPackageManifestPath(string packagePath)
        {
            return packagePath + "/" + kPackageManifestFileName;
        }

        private static void UpdateProjectManifest(PackageRegistry[] addRegistries)
        {
            var     projectManifestDict     = GetManifestObject(kProjectManifestFilePath);

            // add new scoped registries to manifest
            if (!projectManifestDict.Contains(ProjectManifestKey.kScopedRegistries))
            {
                projectManifestDict[ProjectManifestKey.kScopedRegistries]   = new List<IDictionary>();
            }
            var     projectScopedRegistries = ConvertIListItems(projectManifestDict[ProjectManifestKey.kScopedRegistries] as IList, (IDictionary item) => ConvertJsonObjectToPackageRegistry(item));
            foreach (var registry in addRegistries)
            {
                var     existingRegistry    = projectScopedRegistries.Find((item) => string.Equals(registry.Name, item.Name));
                if (existingRegistry != null)
                {
                    MergeRegistryScopes(fromRegistry: registry, toRegistry: existingRegistry);
                }
                else
                {
                    projectScopedRegistries.Add(registry.Clone());
                }
            }
            projectManifestDict[ProjectManifestKey.kScopedRegistries]   = projectScopedRegistries.ConvertAll((item) => ConvertPackageRegistryToJsonObject(item));

            // commit new changes
            var     fileText        = JsonConvert.SerializeObject(projectManifestDict, Formatting.Indented);
            File.WriteAllText(kProjectManifestFilePath, fileText);
        }

        private static IDictionary GetManifestObject(string path)
        {
            var     manifestText    = File.ReadAllText(path);
            return JsonConverterUtility.DeserializeObject(manifestText) as IDictionary;
        }

        private static PackageRegistry[] GetScopedRegistries(string package)
        {
            var     manifestPath    = GetPackageManifestPath(package);
            var     manifestDict    = GetManifestObject(path: manifestPath);
            if (manifestDict.Contains(PackageManifestKey.kScopedRegistries))
            {
                var     registries          = new List<PackageRegistry>();
                var     registriesJson      = manifestDict[PackageManifestKey.kScopedRegistries] as IList;
                foreach (var registryDict in registriesJson)
                {
                    var     registryObject  = ConvertJsonObjectToPackageRegistry((IDictionary)registryDict);
                    registries.Add(registryObject);
                }
                return registries.ToArray();
            }
            return null;
        }

        private static void MergeRegistryScopes(PackageRegistry fromRegistry, PackageRegistry toRegistry)
        {
            foreach (var scope in fromRegistry.Scopes)
            {
                toRegistry.AddScope(scope);
            }
        }

        #endregion

        #region Manage registry methods

        private static void OnResolveRegistriesUpdate()
        {
            if (s_getPackagesRequest.IsCompleted)
            {
                var     usedPackages    = new List<string>();
                if (s_getPackagesRequest.Status == StatusCode.Success)
                {
                    foreach (var package in s_getPackagesRequest.Result)
                    {
                        usedPackages.Add(package.resolvedPath);
                    }
                }

                // add custom registry to project manifest
                if (usedPackages.Count != 0)
                {
                    var     newRegistries   = new List<PackageRegistry>();
                    foreach (var package in usedPackages)
                    {
                        var     registries  = GetScopedRegistries(package);
                        if (registries != null)
                        {
                            newRegistries.AddRange(registries);
                        }
                    }

                    if (newRegistries.Count != 0)
                    {
                        UpdateProjectManifest(newRegistries.ToArray());
                    }
                }
                OnResolveRegistriesEnd();
            }
        }

        private static void OnResolveRegistriesEnd()
        {
            EditorApplication.update   -= OnResolveRegistriesUpdate;
            SetIsBusy(value: false);
        }

        #endregion

        #region Converter methods

        private static PackageRegistry ConvertJsonObjectToPackageRegistry(IDictionary registryDict)
        {
            var     registry    = new PackageRegistry(
                name: registryDict["name"] as string,
                url: registryDict["url"] as string);
            if (registryDict.Contains("scopes"))
            {
                foreach (var scope in registryDict["scopes"] as IList)
                {
                    registry.AddScope(scope as string);
                }
            }
            return registry;
        }

        private static IDictionary ConvertPackageRegistryToJsonObject(PackageRegistry registry)
        {
            return new Dictionary<string, object>()
            {
                { "name", registry.Name },
                { "url", registry.Url },
                { "scopes", registry.Scopes }
            };
        }

        private static List<TOutput> ConvertIListItems<TInput, TOutput>(IList list, System.Converter<TInput, TOutput> function)
        {
            var     outputList      = new List<TOutput>();
            foreach (var input in list)
            {
                outputList.Add(function((TInput)input));
            }
            return outputList;
        }

        #endregion

        #region IO methods

        private static void MoveAssetPackageToPackagesFolder(string path)
        {
            string  packageName     = new DirectoryInfo(path).Name;
            Debug.LogFormat("[PackageDependencyManager] Moving package: {0} to Packages folder.", packageName);
            MoveDirectory(path, "Packages/" + packageName);
        }

        private static void MoveDirectory(string source, string destination)
        {
            try
            {
                // ensure that specified folder doesn't exist
                var     destinationDir  = new DirectoryInfo(destination);
                if (destinationDir.Exists)
                {
                    destinationDir.Delete(true);
                }
                else
                {
                    var     parentDir   = Directory.GetParent(destination);
                    if (!parentDir.Exists)
                    {
                        parentDir.Create();
                    }
                }

                Directory.Move(source, destination);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        #endregion

        #region Menu item methods

        [MenuItem("Assets/Package Manager Services/Install Selected Asset in Packages")]
        private static void MoveSelectedAssetPackage()
        {
            try
            {
                SetIsBusy(value: true);

                var     assetPath   = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                MoveAssetPackageToPackagesFolder(assetPath);

                AssetDatabase.Refresh();
            }
            finally
            {
                SetIsBusy(value: false);
            }
        }

        [MenuItem("Assets/Package Manager Services/Install Selected Asset in Packages", validate = true)]
        private static bool ValidateMoveSelectedAssetPackage()
        {
            if (Selection.assetGUIDs.Length > 0)
            {
                var     assetPath   = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                return assetPath.StartsWith("Assets") && !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path: GetPackageManifestPath(assetPath)));
            }
            return false;
        }

        [MenuItem("Assets/Package Manager Services/Install All Assets in Packages")]
        private static bool MoveAllAssetPackages()
        {
            try
            {
                SetIsBusy(value: true);

                // find all the packages added to Assets folder
                var assetPackages = new List<string>();
                foreach (var assetGuid in AssetDatabase.FindAssets("t:textasset"))
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                    if (assetPath.StartsWith("Assets") && assetPath.EndsWith(kPackageManifestFileName))
                    {
                        assetPackages.Add(assetPath.Substring(0, assetPath.Length - (kPackageManifestFileName.Length + 1)));
                    }
                }

                // move shortlisted packages to Packages folder
                foreach (var packagePath in assetPackages)
                {
                    MoveAssetPackageToPackagesFolder(packagePath);
                }
                AssetDatabase.Refresh();

                return (assetPackages.Count != 0);
            }
            finally
            {
                SetIsBusy(value: false);
            }
        }

        [MenuItem("Assets/Package Manager Services/Resolve Registries")]
        private static void ResolveRegistries()
        {
            SetIsBusy(value: true);

            s_getPackagesRequest        = Client.List();
            EditorApplication.update   += OnResolveRegistriesUpdate;
        }

        #endregion

        #region Editor callback methods

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // disable checks until manager is ready
            if (s_isBusy)
            {
                return;
            }

            // check whether project configuration is dirty
            var     importedPackages        = FindPackages(importedAssets);
            if (importedPackages.Length != 0)
            {
                foreach (var package in importedPackages)
                {
                    if (package.StartsWith("Assets"))
                    {
                        SetProjectDirty();
                        break;
                    }
                }
            }
        }

        #endregion

        #region Nested types

        private enum OperationState : int
        {
            Pending,

            Inprogress,

            Done
        }

        #endregion
    }
}
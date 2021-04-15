using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using Newtonsoft.Json;
using UnityEditor.PackageManager.Requests;
using Newtonsoft.Json.Linq;

namespace VoxelBusters.PackageManagerServices
{
    public class PackageDependencyManager : AssetPostprocessor
    {
        #region Constants

        private     const   string  kPackageManifestFileName    = "package.json";

        private     const   string  kProjectManifestFilePath    = "Packages/Manifest.json";

        #endregion

        #region Static fields

        private     static  ListRequest     s_getPackagesRequest;

        #endregion

        #region Private static methods

        private static string[] FindPackages(string[] assets)
        {
            var     targetAssets    = new List<string>();
            foreach (var item in assets)
            {
                if (item.EndsWith(kPackageManifestFileName))
                {
                    targetAssets.Add(item);
                }
            }
            return targetAssets.ToArray();
        }

        #endregion

        #region Manage manifest methods

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
            var     manifestDict        = GetManifestObject(path: package + "/" + kPackageManifestFileName);
            if (manifestDict.Contains(PackageManifestKey.kScopedRegistries))
            {
                var     registries      = new List<PackageRegistry>();
                var     registriesJson  = manifestDict[PackageManifestKey.kScopedRegistries] as IList;
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
                    UpdateProjectManifest(newRegistries.ToArray());
                }
                OnResolveRegistriesEnd();
            }
        }

        private static void OnResolveRegistriesEnd()
        {
            EditorApplication.update   -= OnResolveRegistriesUpdate;
            EditorApplication.UnlockReloadAssemblies();
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
            Debug.LogFormat("[VoxelBusters] Moving package: {0} to packages folder.", packageName);
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
            var     assetPath   = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            MoveAssetPackageToPackagesFolder(assetPath);

            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Package Manager Services/Install Selected Asset in Packages", validate = true)]
        private static bool ValidateMoveSelectedAssetPackage()
        {
            if (Selection.assetGUIDs.Length > 0)
            {
                var     assetPath   = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                return assetPath.StartsWith("Assets") && !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath + "/" + kPackageManifestFileName));
            }
            return false;
        }

        [MenuItem("Assets/Package Manager Services/Install All Assets in Packages")]
        private static void MoveAllAssetPackages()
        {
            // find all the packages added to Assets folder
            var     assetPackages   = new List<string>();
            foreach (var assetGuid in AssetDatabase.FindAssets("t:textasset"))
            {
                var     assetPath   = AssetDatabase.GUIDToAssetPath(assetGuid);
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
        }

        [MenuItem("Assets/Package Manager Services/Resolve Registries")]
        private static void ResolveRegistries()
        {
            EditorApplication.LockReloadAssemblies();

            s_getPackagesRequest        = Client.List();
            EditorApplication.update   += OnResolveRegistriesUpdate;
        }

        #endregion
    }
}
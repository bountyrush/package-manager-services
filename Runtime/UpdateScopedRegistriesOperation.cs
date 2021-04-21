using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.IO;

namespace BountyRush.PackageManagerServices
{
    public class UpdateScopedRegistriesOperation : AsyncOperationBase<UnityEditor.PackageManager.PackageInfo[]>
    {
        #region Constants

        private     const   string          kManifestFilePath            = "Packages/Manifest.json";

        #endregion

        #region Converter methods

        private static PackageRegistry ConvertJsonObjectToPackageRegistry(IDictionary jsonDict)
        {
            var     registry    = new PackageRegistry(
                name: jsonDict["name"] as string,
                url: jsonDict["url"] as string);
            if (jsonDict.TryGetValue(key: "scopes", value: out IList scopeJsonList))
            {
                foreach (string scope in scopeJsonList)
                {
                    registry.AddScope(scope);
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

        #endregion

        #region Base class methods

        protected override void OnStart()
        {
            base.OnStart();

            // make request to fetch installed packages
            var     getPackagesOp       = new GetPackagesOperation();
            getPackagesOp.OnComplete   += OnGetPackagesComplete;
        }

        #endregion

        #region Manage manifest methods

        private void UpdateProjectManifest(PackageRegistry[] addRegistries)
        {
            var     projectManifestJsonDict = PackageUtility.GetManifestObject(kManifestFilePath);

            // add new scoped registries to manifest
            if (!projectManifestJsonDict.Contains(ProjectManifestKey.kScopedRegistries))
            {
                projectManifestJsonDict[ProjectManifestKey.kScopedRegistries]   = new List<IDictionary>();
            }
            var     projectScopedRegistries = CollectionUtility.ConvertIListItems(projectManifestJsonDict[ProjectManifestKey.kScopedRegistries] as IList, (IDictionary item) => ConvertJsonObjectToPackageRegistry(item));
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
            projectManifestJsonDict[ProjectManifestKey.kScopedRegistries]   = projectScopedRegistries.ConvertAll((item) => ConvertPackageRegistryToJsonObject(item));

            // commit new changes
            var     fileText            = JsonConvert.SerializeObject(projectManifestJsonDict, Formatting.Indented);
            File.WriteAllText(kManifestFilePath, fileText);
        }

        private static PackageRegistry[] GetScopedRegistries(string package)
        {
            var     manifestPath        = PackageUtility.GetPackageManifestPath(package);
            var     manifestJsonDict    = PackageUtility.GetManifestObject(path: manifestPath);
            if (manifestJsonDict.TryGetValue(key: PackageManifestKey.kScopedRegistries, value: out IList registryJsonList))
            {
                var     registries      = new List<PackageRegistry>();
                foreach (IDictionary registryJsonDict in registryJsonList)
                {
                    var     registryObject  = ConvertJsonObjectToPackageRegistry(registryJsonDict);
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

        #region Callback methods

        private void OnGetPackagesComplete(AsyncOperationBase<UnityEditor.PackageManager.PackageInfo[]> asyncOperation)
        {
            // find the packages using scoped registries and add those details to the project manifest
            var     usedPackages        = new List<UnityEditor.PackageManager.PackageInfo>();
            if (asyncOperation.Result.Length != 0)
            {
                var     newRegistries   = new List<PackageRegistry>();
                foreach (var package in asyncOperation.Result)
                {
                    var     registries  = GetScopedRegistries(package.resolvedPath);
                    if (registries != null)
                    {
                        usedPackages.Add(package);
                        newRegistries.AddRange(registries);
                    }
                }

                if (newRegistries.Count != 0)
                {
                    UpdateProjectManifest(newRegistries.ToArray());
                }
            }

            // mark that operation is completed
            SetCompleted(usedPackages.ToArray());
        }

        #endregion
    }
}
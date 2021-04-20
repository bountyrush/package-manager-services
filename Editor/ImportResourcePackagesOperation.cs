using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    public class ImportResourcePackagesOperation : AsyncOperationBase<UnityEditor.PackageManager.PackageInfo[]>
    {
        #region Fields

        private     bool        m_includeOptionals;

        #endregion

        #region Constructors

        public ImportResourcePackagesOperation(bool includeOptionals = false)
        {
            // set properties
            m_includeOptionals  = includeOptionals;
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

        #region Private methods

        private ResourcePackageInfo[] GetResourcePackages(string package, bool includeOptionals)
        {
            var     manifestPath        = PackageUtility.GetPackageManifestPath(package);
            var     manifestJsonDict    = PackageUtility.GetManifestObject(path: manifestPath);
            if (manifestJsonDict.TryGetValue(key: PackageManifestKey.kResourcePackages, value: out IList resourcePackageJsonList))
            {
                var     resourcePackages    = new List<ResourcePackageInfo>();
                foreach (IDictionary item in resourcePackageJsonList)
                {
                    item.TryGetValue(key: "installPath", value: out string installPath, defaultValue: null);
                    item.TryGetValue(key: "optional", value: out bool optional, defaultValue: false);

                    if ((!string.IsNullOrEmpty(installPath) && AssetDatabase.IsValidFolder(installPath)) ||
                        (optional && !includeOptionals))
                    {
                        continue;
                    }

                    var     path            = item["path"] as string;
                    var     isLocalFile     = !path.StartsWith("Packages");
                    var     parentPackage   = isLocalFile ? new DirectoryInfo(package).Name : path.Split('/')[1];
                    var     fullPath        = isLocalFile ? "Packages/" + parentPackage + "/" + path : path;
                    resourcePackages.Add(new ResourcePackageInfo(
                        name: item["name"] as string,
                        path: fullPath,
                        installPath: installPath,
                        parent: parentPackage,
                        isOptional: optional));
                }
                return resourcePackages.ToArray();
            }
            return null;
        }

        private void ImportPackages(ResourcePackageInfo[] resourcePackages, bool needsPermission)
        {
            if (needsPermission)
            {
                var     messageBuilder  = new StringBuilder();
                messageBuilder.AppendLine("System has detected that following resource packages are required for your project.");
                foreach (var item in resourcePackages)
                {
                    messageBuilder.AppendFormat("{0}: {1}", item.Name, item.Parent);
                }

                var     selectedButton  = EditorUtility.DisplayDialog(
                    title: "Import Resources",
                    message: messageBuilder.ToString(),
                    ok: "Import",
                    cancel: "Cancel");
                if (!string.Equals(selectedButton, "Import"))
                {
                    return;
                }
            }

            // import specified packages
            foreach (var item in resourcePackages)
            {
                AssetDatabase.ImportPackage(packagePath: item.Path, interactive: false); 
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
                var     resourcePackages    = new List<ResourcePackageInfo>();
                foreach (var package in asyncOperation.Result)
                {
                    var     definedPackages = GetResourcePackages(package: package.resolvedPath, includeOptionals: m_includeOptionals);
                    if (definedPackages != null)
                    {
                        usedPackages.Add(package);
                        resourcePackages.AddRange(definedPackages);
                    }
                }

                if (resourcePackages.Count != 0)
                {
                    ImportPackages(resourcePackages.ToArray(), needsPermission: true);
                }
            }

            // mark that operation is completed
            SetCompleted(usedPackages.ToArray());
        }

        #endregion

        #region Nested types

        private class ResourcePackageInfo
        {
            #region Properties

            public string Name { get; private set; }

            public string Path { get; private set; }

            public string InstallPath { get; private set; }

            public string Parent { get; private set; }

            public bool IsOptional { get; private set; }

            #endregion

            #region Constructors

            public ResourcePackageInfo(string name, string path, string installPath, string parent, bool isOptional)
            {
                // set properties
                Name            = name;
                Path            = path;
                InstallPath     = installPath;
                Parent          = parent;
                IsOptional      = isOptional;
            }

            #endregion
        }

        #endregion
    }
}
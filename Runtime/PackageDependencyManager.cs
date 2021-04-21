using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace BountyRush.PackageManagerServices
{
    [InitializeOnLoad]
    public class PackageDependencyManager : AssetPostprocessor
    {
        #region Constants

        private     const   string      kStateResolvePackages   = "resolve-packages-state";

        #endregion

        #region Static fields

        private     static  bool        s_isBusy;

        private     static  int         s_isBusyRequestCounter  = 0;

        #endregion

        #region Constructors

        static PackageDependencyManager()
        {
            EditorApplication.delayCall    += AutoResolvePackagesIfRequired;
        }

        #endregion

        #region Private static methods

        private static string[] FindPackages(string[] assets)
        {
            var     targetAssets    = new List<string>();
            foreach (var item in assets)
            {
                if (item.EndsWith(PackageUtility.kPackageManifestFileName))
                {
                    var     parentFolderName    = Path.GetDirectoryName(item);
                    targetAssets.Add(parentFolderName);
                }
            }
            return targetAssets.ToArray();
        }

        private static void SetIsBusy(bool value)
        {
            // based on value, update counter
            s_isBusyRequestCounter  = value ? (s_isBusyRequestCounter + 1) : Mathf.Max(0, s_isBusyRequestCounter - 1);
            
            // update state value
            if (s_isBusy)
            {
                if (s_isBusyRequestCounter == 0)
                {
                    s_isBusy    = false;
                    EditorApplication.UnlockReloadAssemblies();
                }
            }
            else if (s_isBusyRequestCounter == 1)
            {
                s_isBusy    = true;
                EditorApplication.LockReloadAssemblies();
            }
        }

        private static void SetProjectDirty()
        {
            SessionState.EraseInt(kStateResolvePackages);
        }

        #endregion

        #region Manage packages methods

        private static void AutoResolvePackagesIfRequired()
        {
            if (GetResolvePackagesOperationState() != OperationState.Pending)
            {
                return;
            }

            EvaluateAndResolvePackages();
        }

        private static void EvaluateAndResolvePackages()
        {
            // update operation state
            SetResolvePackagesOperationState(OperationState.Inprogress);

            // ensure that asset packages are installed in Packages folder
            MoveAllAssetPackages();

            // find all the custom scoped registries used by the installed packages and add it to project manifest
            AddRegistriesInternal(() =>
            {
                // import essential resources
                var     importOp        = new ImportResourcePackagesOperation();
                importOp.OnComplete    += (op) =>
                {
                    SetResolvePackagesOperationState(OperationState.Done);
                    AssetDatabase.Refresh();
                };
            });
        }

        private static void AddRegistriesInternal(System.Action completionCallback = null)
        {
            Debug.Log("[PackageDependencyManager] Started add-registries operation.");

            // mark that operation is in progress
            SetIsBusy(value: true);

            // start operation
            var     updateOp        = new UpdateScopedRegistriesOperation();
            updateOp.OnComplete    += (op) =>
            {
                Debug.Log("[PackageDependencyManager] Completed add-registries operation.");

                // reset state
                SetIsBusy(value: false);

                // refresh asset database
                AssetDatabase.Refresh();

                // send callback
                completionCallback?.Invoke();
            };
        }

        private static OperationState GetResolvePackagesOperationState()
        {
            return (OperationState)SessionState.GetInt(kStateResolvePackages, defaultValue: (int)OperationState.Pending);
        }

        private static void SetResolvePackagesOperationState(OperationState value)
        {
            SessionState.SetInt(kStateResolvePackages, (int)value);

            if (value == OperationState.Inprogress)
            {
                Debug.Log("[PackageDependencyManager] Started resolve-packages operation.");
                SetIsBusy(true);
            }
            else if (value == OperationState.Done)
            {
                Debug.Log("[PackageDependencyManager] Completed resolve-packages operation.");
                SetIsBusy(false);
            }
        }

        #endregion

        #region IO methods

        private static void MoveAssetPackageToPackagesFolder(string path)
        {
            string  packageName     = new DirectoryInfo(path).Name;
            Debug.LogFormat("[PackageDependencyManager] Moving asset package: {0} to Packages folder.", packageName);
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
            }
            finally
            {
                SetIsBusy(value: false);
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Assets/Package Manager Services/Install Selected Asset in Packages", validate = true)]
        private static bool ValidateMoveSelectedAssetPackage()
        {
            if (Selection.assetGUIDs.Length > 0)
            {
                var     assetPath   = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                return assetPath.StartsWith("Assets") && !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path: PackageUtility.GetPackageManifestPath(assetPath)));
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
                var     assetPackages   = new List<string>();
                foreach (var assetGuid in AssetDatabase.FindAssets("t:textasset"))
                {
                    var     assetPath   = AssetDatabase.GUIDToAssetPath(assetGuid);
                    if (assetPath.StartsWith("Assets") && assetPath.EndsWith(PackageUtility.kPackageManifestFileName))
                    {
                        assetPackages.Add(assetPath.Substring(0, assetPath.Length - (PackageUtility.kPackageManifestFileName.Length + 1)));
                    }
                }

                // move shortlisted packages to Packages folder
                foreach (var packagePath in assetPackages)
                {
                    MoveAssetPackageToPackagesFolder(packagePath);
                }

                return (assetPackages.Count != 0);
            }
            finally
            {
                SetIsBusy(value: false);
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Assets/Package Manager Services/Install All Assets in Packages", validate = true)]
        private static bool ValidateMoveAllAssetPackages()
        {
            return !s_isBusy;
        }

        [MenuItem("Assets/Package Manager Services/Add Registries")]
        private static void AddRegistries()
        {
            AddRegistriesInternal();
        }

        [MenuItem("Assets/Package Manager Services/Add Registries", validate = true)]
        private static bool ValidateAddRegistries()
        {
            return !s_isBusy;
        }

        [MenuItem("Assets/Package Manager Services/Resolve Packages")]
        private static void ResolvePackages()
        {
            EvaluateAndResolvePackages();
        }

        [MenuItem("Assets/Package Manager Services/Resolve Packages", validate = true)]
        private static bool ValidateResolvePackages()
        {
            return !s_isBusy;
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
            var     importedPackages    = FindPackages(importedAssets);
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
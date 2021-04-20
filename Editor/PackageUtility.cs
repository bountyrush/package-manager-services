using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    public static class PackageUtility
    {
        #region Constants

        public  const   string  kPackageManifestFileName    = "package.json";

        #endregion

        #region Static methods

        public static string GetPackageManifestPath(string packagePath)
        {
            return packagePath + "/" + kPackageManifestFileName;
        }

        public static IDictionary GetManifestObject(string path)
        {
            var     manifestFullPath    = Path.GetFullPath(path);
            var     manifestText        = File.ReadAllText(manifestFullPath);
            return JsonConverterUtility.DeserializeObject(manifestText) as IDictionary;
        }

        #endregion
    }
}
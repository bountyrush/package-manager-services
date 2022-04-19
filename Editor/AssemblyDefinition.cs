using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    [System.Serializable]
    public class AssemblyDefinition
    {
        #region Properties

        [JsonProperty("name")]
        public string Name { get; private set; }
        
        [JsonProperty("references")]
        public string[] References { get; private set; }

        [JsonProperty("optionalUnityReferences")]
        public string[] OptionalUnityReferences { get; private set; }
        
        [JsonProperty("includePlatforms")]
        public string[] IncludePlatforms { get; private set; }
        
        [JsonProperty("excludePlatforms")]
        public string[] ExcludePlatforms { get; private set; }
        
        [JsonProperty("allowUnsafeCode")]
        public bool AllowUnsafeCode { get; private set; }
        
        [JsonProperty("overrideReferences")]
        public bool OverrideReferences { get; private set; }
        
        [JsonProperty("precompiledReferences")]
        public string[] PrecompiledReferences { get; private set; }
        
        [JsonProperty("autoReferenced")]
        public bool AutoReferenced { get; private set; }
        
        [JsonProperty("defineConstraints")]
        public string[] DefineConstraints { get; private set; }

        #endregion

        #region Constructors

        public AssemblyDefinition(string name, string[] references = null,
            string[] optionalUnityReferences = null, string[] includePlatforms = null,
            string[] excludePlatforms = null, bool allowUnsafeCode = false,
            bool overrideReferences = false, string[] precompiledReferences = null,
            bool autoReferenced = false, string[] defineConstraints = null)
        {
            // set properties
            Name                    = name;
            References              = references ?? new string[0];
            OptionalUnityReferences = optionalUnityReferences ?? new string[0];
            IncludePlatforms        = includePlatforms ?? new string[0];
            ExcludePlatforms        = excludePlatforms ?? new string[0];
            AllowUnsafeCode         = allowUnsafeCode;
            OverrideReferences      = overrideReferences;
            PrecompiledReferences   = precompiledReferences ?? new string[0];
            AutoReferenced          = autoReferenced;
            DefineConstraints       = defineConstraints ?? new string[0];
        }

        #endregion
    }
}
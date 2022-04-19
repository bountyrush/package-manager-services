using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    [System.Serializable]
    public class PackageDefinition
    {
        #region Properties

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; private set; }

        [JsonProperty("description")]
        public string Description { get; private set; }

        [JsonProperty("version")]
        public string Version { get; private set; }

        [JsonProperty("unity")]
        public string Unity { get; private set; }

        [JsonProperty("dependencies")]
        public Dictionary<string, string> Dependencies { get; private set; }

        [JsonProperty("keywords")]
        public string[] Keywords { get; private set; }

        [JsonProperty("author")]
        public User Author { get; private set; }

        #endregion

        #region Constructors

        public PackageDefinition(string name = "", string displayName = "",
            string description = "", string version = "1.0.0",
            string unity = null, Dictionary<string, string> dependencies = null,
            string[] keywords = null, User author = null)
        {
            // set properties
            Name            = name;
            DisplayName     = displayName;
            Description     = description;
            Version         = version;
            Unity           = unity;
            Dependencies    = dependencies ?? new Dictionary<string, string>();
            Keywords        = keywords ?? new string[0];
            Author          = author;
        }

        #endregion
    }
}
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    [System.Serializable]
    public class User
    {
        #region Properties

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("email")]
        public string Email { get; private set; }

        [JsonProperty("url")]
        public string Url { get; private set; }

        #endregion

        #region Constructors

        public User(string name, string email, string url)
        {
            // set properties
            Name    = name;
            Email   = email;
            Url     = url;
        }

        #endregion
    }
}
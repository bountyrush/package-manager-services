using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    [System.Serializable]
    public class StringKeyValuePair : SerializableKeyValuePair<string, string>
    {
        #region Constructors

        public StringKeyValuePair(string key, string value)
            : base(key, value)
        { }

        #endregion
    }
}

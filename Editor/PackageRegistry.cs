using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    public class PackageRegistry
    {
        #region Fields

        private     List<string>    m_scopes;

        #endregion

        #region Properties

        public string Name { get; private set; }

        public string Url { get; private set; }

        public string[] Scopes { get { return m_scopes.ToArray(); } }

        #endregion

        #region Constructors

        public PackageRegistry(string name, string url, params string[] scopes)
        {
            // set properties
            Name        = name;
            Url         = url;
            m_scopes    = new List<string>();
            if (scopes != null)
            {
                foreach (var item in scopes)
                {
                    AddScope(item);
                }
            }
        }

        #endregion

        #region Public methods

        public bool AddScope(string value)
        {
            if (m_scopes.Contains(value))
            {
                return false;
            }

            m_scopes.Add(value);
            return true;
        }

        public bool RemoveScope(string value)
        {
            return m_scopes.Remove(value);
        }

        public PackageRegistry Clone()
        {
            return new PackageRegistry(Name, Url, Scopes);
        }

        #endregion
    }
}
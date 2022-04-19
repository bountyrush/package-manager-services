using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    [System.Serializable]
    public class SerializableKeyValuePair<TKey, TValue>
    {
        #region Fields

        [SerializeField]
        private     TKey        m_key;

        [SerializeField]
        private     TValue      m_value;

        #endregion

        #region Properties

        public TKey Key
        {
            get => m_key;
            set => m_key    = value;
        }

        public TValue Value
        {
            get => m_value;
            set => m_value  = value;
        }

        #endregion

        #region Constructors

        protected SerializableKeyValuePair(TKey key = default(TKey), TValue value = default(TValue))
        {
            // set properties
            m_key       = key;
            m_value     = value;
        }

        #endregion

        #region Static methods

        public static Dictionary<TKey, TValue> ConvertKeyValuePairsToDictionary(SerializableKeyValuePair<TKey, TValue>[] keyValuePairs)
        {
            var     dict    = new Dictionary<TKey, TValue>();
            foreach (var item in keyValuePairs)
            {
                dict.Add(item.Key, item.Value);
            }
            return dict;
        }

        #endregion
    }
}
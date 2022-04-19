using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    internal static class CollectionUtility
    {
        #region Static methods

        public static List<TOutput> ConvertIListItems<TInput, TOutput>(IList list, System.Converter<TInput, TOutput> function)
        {
            var     outputList      = new List<TOutput>();
            foreach (var input in list)
            {
                outputList.Add(function((TInput)input));
            }
            return outputList;
        }

        public static bool TryGetValue<T>(this IDictionary dict, string key, out T value, T defaultValue = default(T))
        {
            if (dict.Contains(key))
            {
                value   = (T)dict[key];
                return true;
            }
            value   = defaultValue;
            return false;
        }

        #endregion
    }
}
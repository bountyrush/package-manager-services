using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BountyRush.PackageManagerServices
{
    public static class JsonConverterUtility
    {
        #region Static methods

        public static object DeserializeObject(string json)
        {
            var     result  = JsonConvert.DeserializeObject(json);
            return ProcessJsonObject(result);
        }

        #endregion

        #region Private static methods

        private static object ProcessJsonObject(object jsonObject)
        {
            if (jsonObject == null)
            {
                return null;
            }

            if (jsonObject is JObject)
            {
                return ToDictionary((JObject)jsonObject);
            }
            else if (jsonObject is JArray)
            {
                return ToArray((JArray)jsonObject);
            }
            else if (jsonObject is JValue)
            {
                return ((JValue)jsonObject).Value;
            }
            return null;
        }

        private static IDictionary<string, object> ToDictionary(this JObject json)
        {
            var     propertyValuePairs  = json.ToObject<Dictionary<string, object>>();
            ProcessJObjectProperties(propertyValuePairs);
            ProcessJArrayProperties(propertyValuePairs);
            return propertyValuePairs;
        }

        private static void ProcessJObjectProperties(IDictionary<string, object> propertyValuePairs)
        {
            var     objectPropertyNames = (from property in propertyValuePairs
                let propertyName = property.Key
                let value = property.Value
                where value is JObject
                select propertyName).ToList();

            objectPropertyNames.ForEach(propertyName => propertyValuePairs[propertyName] = ToDictionary((JObject) propertyValuePairs[propertyName]));
        }

        private static void ProcessJArrayProperties(IDictionary<string, object> propertyValuePairs)
        {
            var     arrayPropertyNames  = (from property in propertyValuePairs
                let propertyName = property.Key
                let value = property.Value
                where value is JArray
                select propertyName).ToList();

            arrayPropertyNames.ForEach(propertyName => propertyValuePairs[propertyName] = ToArray((JArray) propertyValuePairs[propertyName]));
        }

        private static object[] ToArray(this JArray array)
        {
            return array.ToObject<object[]>().Select(ProcessArrayEntry).ToArray();
        }

        private static object ProcessArrayEntry(object value)
        {
            if (value is JObject)
            {
                return ToDictionary((JObject)value);
            }
            if (value is JArray)
            {
                return ToArray((JArray)value);
            }
            return value;
        }

        #endregion
    }
}

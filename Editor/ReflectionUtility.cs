using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    public static class ReflectionUtility
    {
        public static void InvokeStaticMethod(string method)
        {
            var     methodComponents    = method.Split('.');
            var     methodName          = methodComponents[methodComponents.Length - 1];
            var     classTypeName       = method.Substring(0, method.Length - (methodName.Length + 1));

            // find method
            Type    classType           = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass && string.Equals(type.FullName, classTypeName))
                    {
                        classType   = type;
                        break;
                    }
                }

                if (classType != null)
                {
                    break;
                }
            }

            // invoke method
            if (classType == null)
            {
                Debug.LogErrorFormat("[ReflectionUtility] Could not find method: {0}", method);
                return;
            }
            classType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        }
    }
}
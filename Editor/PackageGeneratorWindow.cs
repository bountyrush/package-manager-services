using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BountyRush.PackageManagerServices
{
    public class PackageGeneratorWindow : EditorWindow
    {
        #region Fields

        [SerializeField]
        private     UnityPackageDefinition      m_package;
        
        [SerializeField]
        private     string                      m_assemblyName;

        [SerializeField]
        private     PackageGeneratorOptions     m_options;

        private     SerializedObject            m_serializedObject;

        #endregion

        #region Static methods

        [MenuItem("Bounty Rush/Package Manager Services/Open Generator")]
        private static void Init()
        {
            var     window      = (PackageGeneratorWindow)GetWindow(typeof(PackageGeneratorWindow));
            window.Show();
        }

        private static T DrawEnumFlagField<T>(SerializedProperty property) where T : System.Enum
        {
            var     oldValue    = (T)System.Enum.ToObject(typeof(T), property.intValue);

            // If "Everything" is set, force Unity to unset the extra bits by iterating through them
            var     newValue    = (T)EditorGUILayout.EnumFlagsField(property.displayName, oldValue);
            if ((int)(object)newValue < 0)
            {
                int     bits    = 0;
                foreach (var enumValue in System.Enum.GetValues(typeof(T)))
                {
                    int     checkBit    = (int)(object)newValue & (int)enumValue;
                    if (checkBit != 0)
                    {
                        bits   |= (int)enumValue;
                    }
                }

                newValue        = (T)(object)bits;
            }
            property.intValue   = (int)(object)newValue;
            return newValue;
        }

        private static void DrawSerializedProperty(SerializedProperty property, bool skipFirstProperty = false,
            bool hasEndProperty = true)
        {
            // move pointer to first element
            var     currentProperty = property.Copy();
            var     endProperty     = default(SerializedProperty);

            // start iterating through the properties
            bool    firstTime       = true;
            while (currentProperty.NextVisible(firstTime))
            {
                if (firstTime)
                {
                    endProperty     = hasEndProperty ? property.GetEndProperty() : null;
                    firstTime       = false;
                    if (skipFirstProperty)
                    {
                        continue;
                    }
                }
                if (hasEndProperty && SerializedProperty.EqualContents(currentProperty, endProperty))
                {
                    break;
                }

                if (currentProperty.hasChildren && currentProperty.propertyType == SerializedPropertyType.Generic)
                {
                    EditorGUILayout.LabelField(currentProperty.displayName);
                    EditorGUI.indentLevel++;
                    DrawSerializedProperty(currentProperty);
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.PropertyField(currentProperty);
                }
            }
        }

        #endregion

        #region Unity methods

        private void OnEnable()
        {
            // set properties
            if (m_package == null)
            {
                m_package           = new UnityPackageDefinition(
                    name: "com.companyname.packagename",
                    version: "1.0.0",
                    unity: Application.unityVersion);
            }
        }

        private void OnGUI()
        {
            if (m_serializedObject == null)
            {
                m_serializedObject  = new SerializedObject(this);
                m_serializedObject.Update();
            }
            var     packageProperty         = m_serializedObject.FindProperty("m_package");
            var     assemblyNameProperty    = m_serializedObject.FindProperty("m_assemblyName");
            var     optionsProperty         = m_serializedObject.FindProperty("m_options");
            DrawSerializedProperty(packageProperty);
            EditorGUILayout.PropertyField(assemblyNameProperty);
            DrawEnumFlagField<PackageGeneratorOptions>(optionsProperty);
            m_serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Generate"))
            {
                PackageGenerator.Generate(
                    path: "Assets",
                    package: m_package,
                    options: m_options,
                    assemblyName: m_assemblyName);
            }
        }

        #endregion

        #region Nested types

        [System.Serializable]
        private class UnityPackageDefinition
        {
            #region Fields

            [SerializeField]
            private     string                  m_name;

            [SerializeField]
            private     string                  m_displayName;

            [SerializeField]
            private     string                  m_description;

            [SerializeField]
            private     string                  m_version;

            [SerializeField]
            private     string                  m_unity;

            [SerializeField]
            private     StringKeyValuePair[]    m_dependencies;

            [SerializeField]
            private     string[]                m_keywords;

            [SerializeField]
            private     UnityUserDefinition     m_author;

            #endregion

            #region Properties

            public string Name => m_name;

            public string DisplayName => m_displayName;

            public string Description => m_description;

            public string Version => m_version;

            public string Unity => m_unity;

            public StringKeyValuePair[] Dependencies => m_dependencies;

            public string[] Keywords => m_keywords;

            public User Author => m_author;

            #endregion

            #region Constructors

            public UnityPackageDefinition(string name, string version, string unity)
            {
                m_name      = name;
                m_version   = version;
                m_unity     = unity;
            }

            #endregion

            #region Static methods

            public static implicit operator PackageDefinition(UnityPackageDefinition definition)
            {
                return new PackageDefinition(
                    name: definition.m_name,
                    displayName: definition.m_displayName,
                    description: definition.m_description,
                    version: definition.m_version,
                    unity: definition.m_unity,
                    dependencies: StringKeyValuePair.ConvertKeyValuePairsToDictionary(definition.m_dependencies),
                    keywords: definition.m_keywords,
                    author: definition.m_author);

            }

            #endregion
        }

        [System.Serializable]
        private class UnityUserDefinition
        {
            #region Fields

            [SerializeField]
            private     string                  m_name;

            [SerializeField]
            private     string                  m_email;

            [SerializeField]
            private     string                  m_url;

            #endregion

            #region Properties

            public string Name => m_name;

            public string Email => m_email;

            public string Url => m_url;

            #endregion

            #region Static methods

            public static implicit operator User(UnityUserDefinition definition)
            {
                return new User(
                    name: definition.m_name,
                    email: definition.m_email,
                    url: definition.m_url);
            }

            #endregion
        }

        #endregion
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BountyRush.PackageManagerServices
{
    public enum PackageGeneratorOptions
    {
        IncludeRuntime = 1,

        IncludeRuntimeTests = 1 << 2,

        IncludeEditor = 1 << 3,

        IncludeEditorTests = 1 << 4,

        IncludeResources = 1 << 5,

        IncludeEditorResources = 1 << 6,

        IncludeDocumentation = 1 << 7,
    }
}
using UnityEngine;
using System;
using System.Linq;
using System.Runtime;

namespace DLN
{
    [Serializable]
    public struct TargetObjectBoundsSettings
    {
        [SerializeField] public ResizeOrientation boundsOrientation;
        //[NoFoldout(NoFoldoutHeaderMode.None)]
        public OptionalBoundsSettings boundsSettings;

        public static TargetObjectBoundsSettings Empty => new TargetObjectBoundsSettings
        {
            boundsOrientation = ResizeOrientation.ObjectToResize,
            boundsSettings = OptionalBoundsSettings.Empty
        };
    }
}

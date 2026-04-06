using UnityEngine;

namespace DLN
{
    [System.Serializable]
    public struct PerAxisMeasurementOptions
    {
        [NoFoldout(NoFoldoutHeaderMode.Label, "Determine X Size By")]
        public MeasureOptions x;
        [NoFoldout(NoFoldoutHeaderMode.Label, "Determine Y Size By")]

        public MeasureOptions y;
        [NoFoldout(NoFoldoutHeaderMode.Label, "Determine Z Size By")]

        public MeasureOptions z;

        public static PerAxisMeasurementOptions Default => new PerAxisMeasurementOptions
        {
            x = MeasureOptions.Default,
            y = MeasureOptions.Default,
            z = MeasureOptions.Default
        };
    }
    [System.Serializable]
    public struct MeasureOptions
    {
        [HideEnumOptionsIfObjectMissing(
               "referenceObject",
               nameof(MeasureSource.ReferenceObject),
               "No Reference Object Present",
               0f)]

        public MeasureSource measureSource;
        [ShowIfEnum(nameof(measureSource), nameof(MeasureSource.Fixed), "Uniform Settings", 12f)]

        public float fixedSize;
        public static MeasureOptions Default => new MeasureOptions
        {
            measureSource = MeasureSource.ReferenceObject,
            fixedSize = .5f

        };
    }

    public enum MeasureSource
    {
        ReferenceObject,
        ObjectToResize,
        Fixed
    }


    [System.Serializable]
    public struct UniformFitOptions
    {
        public IncludeAxis axisToConstrainTo;
        public IncludeAxis axisToConstrain;

    }

    [System.Serializable]
    public struct ResizeOptions
    {
        [InspectorName("Dimensions to Fit To")]
        [SerializeField] public PerAxisMeasurementOptions finalSizeOptions;
        [SerializeField] public IncludeAxis axisToActuallyResize;

        [SerializeField] public bool fitUniformly;
        [ShowIfBool(nameof(fitUniformly), true, "Uniform Fit Options", 12f)]
        [SerializeField] public UniformFitOptions uniformFitOptions;
        public bool resizeMeshOnly;
        public static ResizeOptions Init()
        {
            return new ResizeOptions
            {
                finalSizeOptions = PerAxisMeasurementOptions.Default,
                axisToActuallyResize = new IncludeAxis { x = true, y = true, z = true },
                resizeMeshOnly = false,
                fitUniformly = false
            };
        }
    }



}

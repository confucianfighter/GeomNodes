// using System;
// using UnityEngine;

// namespace DLN
// {
//     [Serializable]
//     public struct ResizeSettings
//     {
//         //[Header("Item(s) to resize")]
//         [SerializeField] public TargetQuery resizeTargets;
//         //[Header("Item(s) to fit to")]
//         [SerializeField] public TargetQuery measureTargets;
        

//         public Vector3 pivot;


//         public ResizeKind resizeKind;

//         public UniformResizeSettings uniform;
//         public NonUniformResizeSettings nonUniform;

//         public static ResizeSettings Default => new ResizeSettings
//         {
//             resizeTargets = TargetQuery.Default,
//             measureTargets = TargetQuery.Default,
//             resizeKind = ResizeKind.Uniform,
//             uniform = UniformResizeSettings.Default,
//             nonUniform = NonUniformResizeSettings.Default

//         };
//     }

//     public enum ResizeKind
//     {
//         Uniform,
//         NonUniform
//     }

//     public enum ResizeMethodOld
//     {
//         ResizeMesh,
//         ScaleTransform
//     }

//     public enum UniformFitType
//     {
//         Encapsulate,
//         FitInside
//     }

//     public enum UniformFitAxisSource
//     {
//         Ignore,
//         Target,
//         Fixed
//     }

//     public enum UniformOutputAxisMode
//     {
//         UniformScaled,
//         Preserve,
//         Fixed
//     }

//     public enum NonUniformAxisMode
//     {
//         Preserve,
//         Target,
//         Fixed
//     }

//     public enum NonUniformResizeMethod
//     {
//         Standard,
//         CornerPreserving
//     }

//     [Serializable]
//     public struct AxisFloat3
//     {
//         public float X;
//         public float Y;
//         public float Z;

//         public AxisFloat3(float x, float y, float z)
//         {
//             X = x;
//             Y = y;
//             Z = z;
//         }

//         public static AxisFloat3 Zero => new AxisFloat3(0f, 0f, 0f);
//         public static AxisFloat3 One => new AxisFloat3(1f, 1f, 1f);

//         public Vector3 ToVector3() => new Vector3(X, Y, Z);
//     }

//     [Serializable]
//     public struct UniformResizeSettings
//     {
//         //[Header("Fit")]
//         public UniformFitType fitType;

//         //[Header("Fit Axis Sources")]
//         public UniformFitAxisSource fitXSource;
//         public UniformFitAxisSource fitYSource;
//         public UniformFitAxisSource fitZSource;

//         [Header("Fixed Fit Sizes")]
//         public AxisFloat3 fixedFitSizes;

//         //[Header("Output Axis Modes")]
//         public UniformOutputAxisMode outputXMode;
//         public UniformOutputAxisMode outputYMode;
//         public UniformOutputAxisMode outputZMode;

//         //[Header("Fixed Output Sizes")]
//         public AxisFloat3 fixedOutputSizes;

//         //[Header("Application")]
//         public ResizeMethodOld resizeMethod;

//         [Tooltip("Relevant mainly when using ScaleTransform.")]
//         public bool scaleIndependentOfChildren;

//         public static UniformResizeSettings Default => new UniformResizeSettings
//         {
//             fitType = UniformFitType.FitInside,

//             fitXSource = UniformFitAxisSource.Target,
//             fitYSource = UniformFitAxisSource.Target,
//             fitZSource = UniformFitAxisSource.Ignore,

//             fixedFitSizes = AxisFloat3.One,

//             outputXMode = UniformOutputAxisMode.UniformScaled,
//             outputYMode = UniformOutputAxisMode.UniformScaled,
//             outputZMode = UniformOutputAxisMode.Preserve,

//             fixedOutputSizes = AxisFloat3.One,

//             resizeMethod = ResizeMethodOld.ScaleTransform,
//             scaleIndependentOfChildren = false
//         };
//     }

//     [Serializable]
//     public struct NonUniformResizeSettings
//     {
//         //[Header("Axis Modes")]
//         public NonUniformAxisMode xMode;
//         public NonUniformAxisMode yMode;
//         public NonUniformAxisMode zMode;

//         //[Header("Fixed Sizes")]
//         public AxisFloat3 fixedSizes;

//         //[Header("Deformation")]
//         public NonUniformResizeMethod resizeMethod;

//         [Range(0f, 0.5f)]
//         [Tooltip("Relevant only when resizeMethod is CornerPreserving.")]
//         public float cornerRegionPercent;

//         public static NonUniformResizeSettings Default => new NonUniformResizeSettings
//         {
//             xMode = NonUniformAxisMode.Target,
//             yMode = NonUniformAxisMode.Target,
//             zMode = NonUniformAxisMode.Preserve,

//             fixedSizes = AxisFloat3.One,

//             resizeMethod = NonUniformResizeMethod.Standard,
//             cornerRegionPercent = 0.25f
//         };
//     }
// }
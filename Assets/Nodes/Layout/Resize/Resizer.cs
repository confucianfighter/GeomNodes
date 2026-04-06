using UnityEngine;
namespace DLN
{
    public static class Resizer
    {
        public static void Resize(ResizeArgs args)
        {
            switch (args.resizeMethod)
            {
                case ResizeMethod.Mesh:
                    args.target.transform.SetScale(Vector3.one,
                        preserveChildPositions: true,
                        preserveChildScales: true,
                        uniformScaleChildren: false);
                    if (args.target.TryGetComponent<C3DLS_PreserveRegionsData>(out var regions))
                    {
                        Debug.Log($"Mesh resize target size is: {args.size}");
                        MeshResizer.ResizeAndPreserveRegions(target: args.target, size: args.size, pivot: args.pivot, preserveRegions: regions.data);
                    }
                    else
                    {
                        MeshResizer.ResizeNormal(target: args.target, size: args.size, pivot: args.pivot);
                    }
                    break;
                case ResizeMethod.Scale:
                    Scale(args);
                    break;
            }
        }
        private static void Scale(ResizeArgs args)
        {
            var target = args.target;
            var targetLocalBounds = target.ToBounds(
                    refOrigin: target.transform,
                    refRotation: target.transform,
                    refScale: target.transform);

            var size = args.size;
            var currentSize = targetLocalBounds.Value.size;
            var currentScale = target.transform.localScale;
            var scaleFactor = size.DivideBy(currentSize);
            target.transform.localScale = scaleFactor.Mul(currentScale);
            Debug.Log($"scaleFactor {scaleFactor}");
            var inverseScale = Vector3.one.DivideBy(scaleFactor);
            Debug.Log($"InverseScale {inverseScale}");
            scaleFactor = scaleFactor.ConstrainUniform().Mul(inverseScale);
            foreach (Transform t in target.transform)
            {
                t.localScale = t.localScale.Mul(scaleFactor);
            }
        }
    }
}
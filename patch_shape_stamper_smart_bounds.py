from pathlib import Path
import sys

ROOT = Path.cwd()
GENERATOR = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperProfileGenerator.cs"


def read(path: Path) -> str:
    if not path.exists():
        raise FileNotFoundError(f"Missing file: {path}")
    return path.read_text(encoding="utf-8")


def write(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8", newline="\n")


def replace_once(text: str, old: str, new: str, where: str) -> str:
    if old not in text:
        raise RuntimeError(f"Could not find expected block in {where}:\n{old[:220]}...")
    return text.replace(old, new, 1)


def main() -> int:
    try:
        text = read(GENERATOR)

        if "public static AdaptiveShapeBuildResult BuildAdaptiveShape(" in text:
            print("ShapeStamperProfileGenerator.cs already appears to have Pass 2 changes.")
            return 0

        # 1) Insert new public build entrypoint right after Generate(...)
        generate_anchor = """        public static void Generate(
            ShapeCanvasDocument shapeDocument,
            ProfileCanvasDocument profileDocument,
            ShapeStampPreviewMaterialSettings materialSettings = null)
        {
            ShapeStampRingBuildResult result = BuildRings(shapeDocument, profileDocument);

            if (result == null)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Failed to build profile rings.");
                return;
            }

            CreateOrUpdateRingPreview(result);

            Mesh segmentedMesh = BuildSegmentedBridgeMesh(result);
            if (segmentedMesh != null)
                CreateOrUpdateSegmentMeshPreview(segmentedMesh, result, materialSettings);

            Debug.Log(
                $"ShapeStamperProfileGenerator: Built {result.OuterRings.Count} outer ring(s), " +
                $"{result.InnerRings.Count} inner ring set(s), " +
                $"{result.ProfileSamples.Count} profile sample(s).");
        }

        public static ShapeStampRingBuildResult BuildRings(
"""
        generate_replacement = """        public static void Generate(
            ShapeCanvasDocument shapeDocument,
            ProfileCanvasDocument profileDocument,
            ShapeStampPreviewMaterialSettings materialSettings = null)
        {
            ShapeStampRingBuildResult result = BuildRings(shapeDocument, profileDocument);

            if (result == null)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Failed to build profile rings.");
                return;
            }

            CreateOrUpdateRingPreview(result);

            Mesh segmentedMesh = BuildSegmentedBridgeMesh(result);
            if (segmentedMesh != null)
                CreateOrUpdateSegmentMeshPreview(segmentedMesh, result, materialSettings);

            Debug.Log(
                $"ShapeStamperProfileGenerator: Built {result.OuterRings.Count} outer ring(s), " +
                $"{result.InnerRings.Count} inner ring set(s), " +
                $"{result.ProfileSamples.Count} profile sample(s).");
        }

        public static AdaptiveShapeBuildResult BuildAdaptiveShape(
            ShapeCanvasDocument shapeDocument,
            ProfileCanvasDocument profileDocument)
        {
            ShapeStampRingBuildResult ringResult = BuildRings(shapeDocument, profileDocument);
            if (ringResult == null)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Failed to build adaptive shape result.");
                return null;
            }

            AdaptiveShapeBuildResult buildResult = new AdaptiveShapeBuildResult
            {
                RingBuildResult = ringResult
            };

            buildResult.SegmentMeshes = BuildSegmentMeshes(ringResult);

            if (ringResult.OuterLoops2D != null &&
                ringResult.OuterLoops2D.Count > 0 &&
                ringResult.ProfileSamples != null &&
                ringResult.ProfileSamples.Count > 0)
            {
                buildResult.StartCapMesh = BuildCapMesh(
                    ringResult.OuterLoops2D[0],
                    ringResult.InnerLoops2D.Count > 0 ? ringResult.InnerLoops2D[0] : null,
                    ringResult.ProfileSamples[0].Z,
                    reverseWinding: true,
                    meshName: "AdaptiveShape_StartCap");

                int last = ringResult.ProfileSamples.Count - 1;
                buildResult.EndCapMesh = BuildCapMesh(
                    ringResult.OuterLoops2D[last],
                    ringResult.InnerLoops2D.Count > 0 ? ringResult.InnerLoops2D[last] : null,
                    ringResult.ProfileSamples[last].Z,
                    reverseWinding: false,
                    meshName: "AdaptiveShape_EndCap");
            }

            return buildResult;
        }

        public static ShapeStampRingBuildResult BuildRings(
"""
        text = replace_once(text, generate_anchor, generate_replacement, GENERATOR.name)

        # 2) Insert new helpers right before CreateOrUpdateRingPreview(...)
        helper_anchor = """        private static void CreateOrUpdateRingPreview(ShapeStampRingBuildResult result)
"""
        helper_block = """        private static List<Mesh> BuildSegmentMeshes(ShapeStampRingBuildResult result)
        {
            List<Mesh> segmentMeshes = new List<Mesh>();

            if (result == null || result.OuterRings == null || result.OuterRings.Count < 2)
                return segmentMeshes;

            int segmentCount = result.OuterRings.Count - 1;
            bool hasInnerRings =
                result.InnerRings != null &&
                result.InnerRings.Count == result.OuterRings.Count;

            for (int i = 0; i < segmentCount; i++)
            {
                List<Vector3> outerA = result.OuterRings[i];
                List<Vector3> outerB = result.OuterRings[i + 1];

                List<Vector3> innerA = hasInnerRings ? result.InnerRings[i] : null;
                List<Vector3> innerB = hasInnerRings ? result.InnerRings[i + 1] : null;

                Mesh segmentMesh = BuildSingleBridgeMesh(
                    outerA,
                    outerB,
                    innerA,
                    innerB,
                    i);

                if (segmentMesh != null)
                    segmentMeshes.Add(segmentMesh);
            }

            return segmentMeshes;
        }

        private static Mesh BuildSingleBridgeMesh(
            List<Vector3> outerA,
            List<Vector3> outerB,
            List<Vector3> innerA,
            List<Vector3> innerB,
            int segmentIndex)
        {
            if (outerA == null || outerB == null)
                return null;

            if (outerA.Count < 3 || outerB.Count < 3)
                return null;

            SegmentedMeshBuilder builder = new SegmentedMeshBuilder();
            int submeshIndex = builder.AddSubmesh();

            AddRingBridge(builder, outerA, outerB, submeshIndex);

            if (innerA != null && innerB != null &&
                innerA.Count >= 3 && innerB.Count >= 3)
            {
                AddRingBridge(builder, innerB, innerA, submeshIndex);
            }

            return builder.ToMesh($"AdaptiveShape_Segment_{segmentIndex:000}");
        }

        private static Mesh BuildCapMesh(
            List<Vector2> outerLoop,
            List<Vector2> innerLoop,
            float z,
            bool reverseWinding,
            string meshName)
        {
            if (outerLoop == null || outerLoop.Count < 3)
                return null;

            SegmentedMeshBuilder builder = new SegmentedMeshBuilder();
            int submeshIndex = builder.AddSubmesh();

            AddCap(
                builder,
                outerLoop,
                innerLoop,
                z,
                submeshIndex,
                reverseWinding);

            return builder.ToMesh(meshName);
        }

        private static void CreateOrUpdateRingPreview(ShapeStampRingBuildResult result)
"""
        text = replace_once(text, helper_anchor, helper_block, GENERATOR.name)

        write(GENERATOR, text)

    except Exception as exc:
        print(f"Patch failed: {exc}", file=sys.stderr)
        return 1

    print("Patched ShapeStamperProfileGenerator.cs")
    print()
    print("What changed:")
    print("- Added BuildAdaptiveShape(...)")
    print("- Added BuildSegmentMeshes(...)")
    print("- Added BuildSingleBridgeMesh(...)")
    print("- Added BuildCapMesh(...)")
    print()
    print("Next checks:")
    print("1. Let Unity recompile.")
    print("2. Confirm no compile errors in ShapeStamperProfileGenerator.")
    print("3. Optionally call BuildAdaptiveShape(...) from a quick test or temp button and inspect SegmentMeshes.Count.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
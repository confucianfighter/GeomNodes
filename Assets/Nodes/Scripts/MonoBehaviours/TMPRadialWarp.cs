using UnityEngine;
using TMPro;

public class RadialWarpText : MonoBehaviour
{
    private TMP_Text textComp;

    [Tooltip("Drag in the Transform you want to warp around")]
    public Transform focalPoint;

    void Awake()
    {
        textComp = GetComponent<TMP_Text>();
        // Ensure the hasChanged flags start clean
        transform.hasChanged = false;
        if (focalPoint != null) focalPoint.hasChanged = false;
    }

    void Update()
    {
        bool textChanged = textComp.havePropertiesChanged;
        bool selfMoved = transform.hasChanged;
        bool focalMoved = (focalPoint != null && focalPoint.hasChanged);

        if (textChanged || selfMoved || focalMoved)
        {
            ApplyWarp();
            // reset all flags
            textComp.havePropertiesChanged = false;
            transform.hasChanged = false;
            if (focalPoint != null) focalPoint.hasChanged = false;
        }
    }

    /// <summary>
    /// Call this at runtime to swap in a new focal Transform and immediately warp.
    /// </summary>
    public void SetFocalTarget(Transform t)
    {
        focalPoint = t;
        if (focalPoint != null) focalPoint.hasChanged = false;
        ApplyWarp();
    }

    private void ApplyWarp()
    {
        if (focalPoint == null) return;

        textComp.ForceMeshUpdate();
        var info = textComp.textInfo;
        int count = info.characterCount;
        if (count == 0) return;

        Vector3 origin = transform.position;
        Vector3 toFocal = focalPoint.position - origin;
        float r = toFocal.magnitude;
        Vector3 dir = toFocal.normalized;

        for (int i = 0; i < count; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;

            int matIdx = info.characterInfo[i].materialReferenceIndex;
            int vertIdx = info.characterInfo[i].vertexIndex;
            Vector3[] verts = info.meshInfo[matIdx].vertices;

            for (int j = 0; j < 4; j++)
            {
                Vector3 worldPos = transform.TransformPoint(verts[vertIdx + j]);
                float proj = Vector3.Dot(worldPos - origin, dir);
                Vector3 offset = worldPos - origin - dir * proj;
                Vector3 warped = origin + dir * r + offset;
                verts[vertIdx + j] = transform.InverseTransformPoint(warped);
            }
        }

        for (int i = 0; i < info.meshInfo.Length; i++)
            textComp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}

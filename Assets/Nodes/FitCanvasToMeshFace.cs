using UnityEngine;

public class FitCanvasToMeshFace : MonoBehaviour
{
    // The position of the mesh face in world space.
    public Vector3 facePosition;
    // The normal of the mesh face.
    public Vector3 faceNormal;

    // The canvas RectTransform to orient. If not assigned, it will use the current object's RectTransform.
    public RectTransform canvasRectTransform;
    // (Optional) A regular world transform to parent all canvas transforms.
    public Transform canvasWorldParent;

    void Start()
    {
        if (canvasRectTransform == null)
        {
            canvasRectTransform = GetComponent<RectTransform>();
        }
        // If a world parent is provided, reparent the canvas to it, preserving the world position.
        if (canvasWorldParent != null)
        {
            canvasRectTransform.SetParent(canvasWorldParent, true);
        }
        OrientCanvas();
    }

    // Orients the canvas so that it lies on the plane defined by facePosition and faceNormal.
    public void OrientCanvas()
    {
        Vector3 n = faceNormal.normalized;

        // Use Vector3.up as a baseline reference.
        Vector3 referenceUp = Vector3.up;
        Vector3 right = Vector3.Cross(referenceUp, n);
        // If faceNormal is collinear with referenceUp, pick an alternative reference vector.
        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.Cross(Vector3.forward, n);
        }
        right.Normalize();

        // Compute the corrected up vector using the cross product.
        Vector3 correctedUp = Vector3.Cross(n, right);

        // Create a rotation where the forward direction is aligned with the face normal
        // and the up direction is the computed correctedUp.
        Quaternion targetRotation = Quaternion.LookRotation(n, correctedUp);

        // Apply the rotation and position to the canvas.
        canvasRectTransform.rotation = targetRotation;
        canvasRectTransform.position = facePosition;
    }

    // Optional: update the face parameters at runtime.
    public void SetFace(Vector3 newFacePosition, Vector3 newFaceNormal)
    {
        facePosition = newFacePosition;
        faceNormal = newFaceNormal;
        OrientCanvas();
    }
}

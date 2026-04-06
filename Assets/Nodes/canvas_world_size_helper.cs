using NUnit.Framework.Constraints;
using UnityEngine;

public class CanvasCornerDebugger : MonoBehaviour
{
    
    // Reference to a cube prefab that you want to place at each corner.
    [SerializeField]
    public GameObject cubePrefab;
    [SerializeField]
    public Transform localToTransform; // none to use world space.

    void Start()
    {
        // Get the RectTransform of the Canvas (or any UI element).
        RectTransform rectTransform = GetComponent<RectTransform>();

        // Create an array to hold the 4 corner positions.
        Vector3[] worldCorners = new Vector3[4];

        // This fills worldCorners with the world positions of the 4 corners.
        rectTransform.GetWorldCorners(worldCorners);

        // Optionally, log the corner positions.
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Debug.Log("Corner " + i + ": " + worldCorners[i]);
        }

        // Instantiate a cube at each corner to verify their positions.
        foreach (Vector3 corner in worldCorners)
        {
            Instantiate(cubePrefab, corner, Quaternion.identity);
        }
    }
    void GetCornersLocalToTransform(Vector3[] worldCorners)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.GetWorldCorners(worldCorners);
        for (int i = 0; i < worldCorners.Length; i++)
        {
            worldCorners[i] = localToTransform.InverseTransformPoint(worldCorners[i]);
        }
    }
}


using CodeSmile.GraphMesh;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Triangle : MonoBehaviour
{
    void Start()
    {
        using var gMesh = GMesh.Triangle();           // create a triangle in the XZ plane
        GetComponent<MeshFilter>().sharedMesh = gMesh.ToMesh();
        // gMesh is disposed automatically because of the using block
    }
}
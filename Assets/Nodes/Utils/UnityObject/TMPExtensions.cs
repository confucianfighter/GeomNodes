// using UnityEngine;
// using TMPro;
// using System.Collections.Generic;
// using System.Linq;
// using System;
// using System.Collections;
// using System.Text;
// using UnityEngine.ProBuilder;
// using vecs = DLN.VectorUtils;

// namespace DLN
// {
//     public static class TMPExtensions
//     {
//         /// <summary>
//         /// Retrieves the world-space positions of the four corners of this TMP_Text UI element.
//         /// Outputs them in the order: bottom-left, top-left, top-right, bottom-right.
//         /// </summary>
//         /// 
//         /// 
//         /// <summary>
//         /// Populates a list of world-space corners for each visible glyph in a TMP_Text component.
//         /// Compatible with Visual Scripting: no return, uses out parameters only.
//         /// </summary>
//         /// <param name="textComponent">The TextMeshPro or TextMeshProUGUI to sample.</param>
//         /// <param name="glyphWorldCorners">Out list where each inner list contains 4 world-space corners of a glyph.</param>
//         public static void GetMaxGlyphWorldSize(TMP_Text textComponent, out float maxWidth, out float maxHeight)
//         {
//             maxWidth = 0f;
//             maxHeight = 0f;
//             if (textComponent == null)
//                 return;

//             // Build printable ASCII range 32 to 126
//             var printable = new StringBuilder();
//             for (int c = 32; c <= 126; c++) printable.Append((char)c);
//             string characterSet = printable.ToString();

//             // Instantiate hidden clone
//             var cloneGO = GameObject.Instantiate(textComponent.gameObject);
//             cloneGO.hideFlags = HideFlags.HideAndDontSave;
//             var cloneTMP = cloneGO.GetComponent<TMP_Text>();

//             cloneTMP.text = characterSet;
//             cloneTMP.ForceMeshUpdate();

//             var info = cloneTMP.textInfo;
//             float maxLocalWidth = 0f;
//             float maxLocalHeight = 0f;

//             for (int i = 0; i < info.characterCount; i++)
//             {
//                 var ci = info.characterInfo[i];
//                 if (!ci.isVisible) continue;

//                 // Local-space height
//                 float localHeight = ci.ascender - ci.descender;
//                 if (localHeight > maxLocalHeight)
//                     maxLocalHeight = localHeight;

//                 // Local-space width via vertex bounds
//                 var verts = info.meshInfo[ci.materialReferenceIndex].vertices;
//                 float minX = float.PositiveInfinity;
//                 float maxX = float.NegativeInfinity;
//                 for (int v = 0; v < 4; v++)
//                 {
//                     float x = verts[ci.vertexIndex + v].x;
//                     if (x < minX) minX = x;
//                     if (x > maxX) maxX = x;
//                 }

//                 float localWidth = maxX - minX;
//                 if (localWidth > maxLocalWidth)
//                     maxLocalWidth = localWidth;
//             }

//             // Convert to world units
//             Vector3 scale = cloneGO.transform.lossyScale;
//             maxWidth = maxLocalWidth * scale.x;
//             maxHeight = maxLocalHeight * scale.y;

//             // Clean up
//             GameObject.DestroyImmediate(cloneGO);
//         }
//         public static void GetMaxDigitGlyphWorldSize(TMP_Text textComponent, out float maxWidth, out float maxHeight)
//         {
//             maxWidth = 0f;
//             maxHeight = 0f;
//             if (textComponent == null)
//                 return;

//             // Build printable ASCII range 32 to 126
//             var printable = new StringBuilder();
//             printable.Append("0123456789+-.");
//             string characterSet = printable.ToString();

//             // Instantiate hidden clone
//             var cloneGO = GameObject.Instantiate(textComponent.gameObject);
//             cloneGO.hideFlags = HideFlags.HideAndDontSave;
//             var cloneTMP = cloneGO.GetComponent<TMP_Text>();

//             cloneTMP.text = characterSet;
//             cloneTMP.ForceMeshUpdate();

//             var info = cloneTMP.textInfo;
//             float maxLocalWidth = 0f;
//             float maxLocalHeight = 0f;

//             for (int i = 0; i < info.characterCount; i++)
//             {
//                 var ci = info.characterInfo[i];
//                 if (!ci.isVisible) continue;


//                 // Local-space width via vertex bounds
//                 var verts = info.meshInfo[ci.materialReferenceIndex].vertices;
//                 int vi = ci.vertexIndex;
//                 var bl = verts[vi + 0];
//                 var tl = verts[vi + 1];
//                 var tr = verts[vi + 2];
//                 var br = verts[vi + 3];

//                 vecs.GetMinMax(
//                     new Vector3[] { bl, tl, tr, br },
//                     out var min, out var max
//                     );

//                 float localWidth = max.x - min.x;
//                 float localHeight = max.y - min.y;
//                 if (localWidth > maxLocalWidth)
//                     maxLocalWidth = localWidth;
//                 if (localHeight > maxLocalHeight)
//                     maxLocalHeight = localHeight;
//             }

//             // Convert to world units
//             Vector3 scale = cloneGO.transform.lossyScale;
//             maxWidth = maxLocalWidth * scale.x;
//             maxHeight = maxLocalHeight * scale.y;

//             // Clean up
//             GameObject.DestroyImmediate(cloneGO);
//         }

//         public static void CreateCharTile(float Height, out Vector3 size, out GameObject tile, TMP_Text tmp, Material mat, float percentDepth = 0.05f)
//         {
//             // initialize out size
//             size = Vector3.one;
//             // Get largest number glyph
//             GetMaxDigitGlyphWorldSize(
//                 tmp,
//                 maxWidth: out float w,
//                 maxHeight: out float h
//                 );
//             // see how target height compares with digit height
//             var scaleFactor = Height / h;
//             // resize tmp.
//             tmp.transform.localScale = tmp.transform.localScale * scaleFactor;

//             // calculate cube tile dimensions
//             size.x = scaleFactor * w;
//             size.y = Height;
//             size.z = Height * percentDepth;
//             // create the tile cube
//             ProBuilderUtils.Cube(
//                 cube: out tile,
//                 size: size,
//                 position: Vector3.one,
//                 pivotLocation: PivotLocation.Center,
//                 material: mat);
//             // set initial text to 1.
//             tmp.SetText(1f.ToString());
//             // force update so centroid is accurate
//             tmp.ForceMeshUpdate();
//             // get centroid for an anchor point
//             TMPExtensions.GetGlyphCentroid(
//                 textComponent: tmp,
//                 centroid: out var glyphCentroid
//                 );
//             // get back panel of tile. Remember, tmp faces opposite so that left and right matches left to right reading direction
//             Bnds.GetBoundsPanel(
//                 @object: tile,
//                 centroid: out var tileCentroid,
//                 normal: out _,
//                 corners: out _,
//                 side: Side.Back,
//                 space: Space.World);
//             // move center of tmp over center of tile
//             TransformUtils.MoveWithAnchor(
//                 @object: tmp,
//                 anchorPoint: glyphCentroid,
//                 targetPoint: tileCentroid,
//                 outPosition: out _);
//             // set tile as parent of tmp
//             tmp.transform.SetParent(
//                 parent: tile.transform,
//                 worldPositionStays: true);
//         }
//         public static void GetAllGlyphWorldCorners(TMP_Text textComponent, out List<List<Vector3>> glyphWorldCorners)
//         {
//             glyphWorldCorners = new List<List<Vector3>>();
//             if (textComponent == null)
//                 return;

//             // Force update to ensure mesh data is current
//             textComponent.ForceMeshUpdate();
//             var textInfo = textComponent.textInfo;
//             var matrix = textComponent.transform.localToWorldMatrix;

//             // Iterate through each character
//             for (int i = 0; i < textInfo.characterCount; i++)
//             {
//                 var ci = textInfo.characterInfo[i];
//                 if (!ci.isVisible)
//                     continue;

//                 var verts = textInfo.meshInfo[ci.materialReferenceIndex].vertices;
//                 var corners = new List<Vector3>(4);

//                 // Collect the 4 corner vertices
//                 for (int v = 0; v < 4; v++)
//                 {
//                     Vector3 localPos = verts[ci.vertexIndex + v];
//                     Vector3 worldPos = matrix.MultiplyPoint3x4(localPos);
//                     corners.Add(worldPos);
//                 }

//                 glyphWorldCorners.Add(corners);
//             }
//         }

//         public static void GetGlyphBounds(TMP_Text tmp, out Vector3 size, out Vector3 extents, out Vector3 center, out Vector3 min, out Vector3 max, int idx = 0)
//         {
//             GetSingleGlyphCorners(tmp, out var corners);
//             GetGlyphCentroid(tmp, out var centroid);
//             var bounds = new Bounds(centroid, Vector3.zero);
//             foreach (var corner in corners)
//             {
//                 bounds.Encapsulate(corner);
//             }

//             size = bounds.size;
//             center = bounds.center;
//             extents = bounds.extents;
//             min = bounds.min;
//             max = bounds.max;
//         }

//         public static void GetSingleGlyphCorners(TMP_Text textComponent, out List<Vector3> corners, int idx = 0)
//         {
//             GetAllGlyphWorldCorners(textComponent, out var cornersList);
//             corners = cornersList[idx];
//         }

//         public static void GetGlyphCentroid(TMP_Text textComponent, out Vector3 centroid, int idx = 0)
//         {
//             GetSingleGlyphCorners(
//                 textComponent: textComponent,
//                 idx: idx,
//                 corners: out var corners);
//             // Get min and max.
//             var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
//             var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
//             foreach (var corner in corners)
//             {
//                 min.x = Mathf.Min(min.x, corner.x);
//                 min.y = Mathf.Min(min.y, corner.y);
//                 min.z = Mathf.Min(min.z, corner.z);

//                 max.x = Mathf.Max(max.x, corner.x);
//                 max.y = Mathf.Max(max.y, corner.y);
//                 max.z = Mathf.Max(max.z, corner.z);
//             }

//             centroid = (min + max) / 2;
//         }

//         public static TMP_Text GetTMPText(this UnityEngine.Object obj)
//         {
//             if (obj is TMP_Text t)
//             {
//                 return t;
//             }
//             else if (obj is GameObject go)
//             {
//                 return go.GetComponent<TMP_Text>();
//             }
//             else if (obj is MonoBehaviour mb)
//             {
//                 return mb.GetComponent<TMP_Text>();
//             }
//             else
//             {
//                 Debug.LogError("Object is not a TMP_Text or GameObject.");
//                 return null;
//             }
//         }
//         public static void SetText(this UnityEngine.Object obj, string text)
//         {
//             TMP_Text t = obj.GetTMPText();
//             t.text = text;

//         }
//         public static void GetText(this UnityEngine.Object obj, out string text)
//         {
//             text = "";
//             TMP_Text t = obj.GetTMPText();
//             if (t != null)
//             {
//                 text = t.text;
//             }
//         }
//         public static void GetWorldCorners(
//             this TMP_Text t,
//             out Vector3 bottomLeft,
//             out Vector3 topLeft,
//             out Vector3 topRight,
//             out Vector3 bottomRight
//         )
//         {
//             var corners = new Vector3[4];
//             t.GetComponent<RectTransform>().GetWorldCorners(corners);
//             bottomLeft = corners[0]; // BL
//             topLeft = corners[1]; // TL
//             topRight = corners[2]; // TR
//             bottomRight = corners[3]; // BR
//         }

//         /// <summary>
//         /// Gets the four corners in world space, then converts them into the local space of externalTransform.
//         /// Outputs: bottom-left, top-left, top-right, bottom-right (all in externalTransform’s local coords).
//         /// </summary>
//         public static void GetCornersLocalToExternalTransform(
//             this TMP_Text t,
//             Transform externalTransform,
//             out Vector3 bottomLeft,
//             out Vector3 topLeft,
//             out Vector3 topRight,
//             out Vector3 bottomRight
//         )
//         {
//             // 1. World corners:
//             t.GetWorldCorners(out Vector3 worldBL, out Vector3 worldTL, out Vector3 worldTR, out Vector3 worldBR);

//             // 2. Convert each to externalTransform’s local space:
//             bottomLeft = externalTransform.InverseTransformPoint(worldBL);
//             topLeft = externalTransform.InverseTransformPoint(worldTL);
//             topRight = externalTransform.InverseTransformPoint(worldTR);
//             bottomRight = externalTransform.InverseTransformPoint(worldBR);
//         }
//         public static void GetTMPMeshLocalBounds(
//             TMP_Text textComponent,
//             out Bounds bounds,
//             bool visibleCharactersOnly = true)
//         {
//             bounds = default;

//             if (textComponent == null)
//             {
//                 bounds = new Bounds(Vector3.zero, Vector3.zero);
//                 return;
//             }

//             textComponent.ForceMeshUpdate();
//             var textInfo = textComponent.textInfo;

//             bool hasAny = false;
//             Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
//             Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

//             for (int i = 0; i < textInfo.characterCount; i++)
//             {
//                 var ci = textInfo.characterInfo[i];
//                 if (visibleCharactersOnly && !ci.isVisible)
//                     continue;

//                 if (!ci.isVisible)
//                     continue; // invisible chars have no useful quad

//                 var verts = textInfo.meshInfo[ci.materialReferenceIndex].vertices;
//                 int vi = ci.vertexIndex;

//                 for (int v = 0; v < 4; v++)
//                 {
//                     Vector3 p = verts[vi + v];

//                     min = Vector3.Min(min, p);
//                     max = Vector3.Max(max, p);
//                     hasAny = true;
//                 }
//             }

//             if (!hasAny)
//             {
//                 bounds = new Bounds(Vector3.zero, Vector3.zero);
//                 return;
//             }

//             bounds = new Bounds((min + max) * 0.5f, max - min);
//         }
//         /// <summary>
//         /// Returns axis-aligned size of the TMP_Text’s RectTransform in world space:
//         /// xSize = width, ySize = height, zSize = depth (usually zero for a flat UI).
//         /// </summary>
//         /// 
//         public static void GetWorldSizeByWorldCorners(
//             this TMP_Text t,
//             out float xSize,
//             out float ySize,
//             out float zSize,
//             out Vector3 size
//         )
//         {
//             t.GetWorldCorners(out Vector3 worldBL, out Vector3 worldTL, out Vector3 worldTR, out Vector3 worldBR);

//             // Compute axis-aligned min/max across all four corners:
//             float minX = Mathf.Min(worldBL.x, worldTL.x, worldTR.x, worldBR.x);
//             float maxX = Mathf.Max(worldBL.x, worldTL.x, worldTR.x, worldBR.x);
//             float minY = Mathf.Min(worldBL.y, worldTL.y, worldTR.y, worldBR.y);
//             float maxY = Mathf.Max(worldBL.y, worldTL.y, worldTR.y, worldBR.y);
//             float minZ = Mathf.Min(worldBL.z, worldTL.z, worldTR.z, worldBR.z);
//             float maxZ = Mathf.Max(worldBL.z, worldTL.z, worldTR.z, worldBR.z);

//             xSize = maxX - minX;
//             ySize = maxY - minY;
//             zSize = maxZ - minZ;
//             size = new Vector3(xSize, ySize, zSize);
//         }

//         /// <summary>
//         /// Computes an axis-aligned Bounds (relative to externalTransform) that encloses the TMP_Text’s rectangle.
//         /// Also outputs xSize/ ySize/ zSize (width/height/depth), plus the computed min and max points.
//         /// </summary>
//         public static void GetBoundsByWorldCornersRelativeToExternalTransform(
//             this TMP_Text t,
//             Transform externalTransform,
//             out Bounds bounds,
//             out float xSize,
//             out float ySize,
//             out float zSize,
//             out Vector3 min,
//             out Vector3 max
//         )
//         {
//             // 1. Get the four corners in externalTransform’s local space:
//             t.GetCornersLocalToExternalTransform(
//                 externalTransform,
//                 out Vector3 blLocal,
//                 out Vector3 tlLocal,
//                 out Vector3 trLocal,
//                 out Vector3 brLocal
//             );

//             // 2. Compute the axis-aligned min/max:
//             float minX = Mathf.Min(blLocal.x, tlLocal.x, trLocal.x, brLocal.x);
//             float maxX = Mathf.Max(blLocal.x, tlLocal.x, trLocal.x, brLocal.x);
//             float minY = Mathf.Min(blLocal.y, tlLocal.y, trLocal.y, brLocal.y);
//             float maxY = Mathf.Max(blLocal.y, tlLocal.y, trLocal.y, brLocal.y);
//             float minZ = Mathf.Min(blLocal.z, tlLocal.z, trLocal.z, brLocal.z);
//             float maxZ = Mathf.Max(blLocal.z, tlLocal.z, trLocal.z, brLocal.z);

//             min = new Vector3(minX, minY, minZ);
//             max = new Vector3(maxX, maxY, maxZ);

//             // 3. Size = max - min. Guard against zero extents:
//             Vector3 sizeVec = new Vector3(
//                 maxX - minX,
//                 maxY - minY,
//                 maxZ - minZ
//             );

//             if (Mathf.Approximately(sizeVec.x, 0f))
//             {
//                 Debug.LogError("Width computed as zero; bumping to epsilon.");
//                 sizeVec.x = 0.00001f;
//             }
//             if (Mathf.Approximately(sizeVec.y, 0f))
//             {
//                 Debug.LogError("Height computed as zero; bumping to epsilon.");
//                 sizeVec.y = 0.00001f;
//             }
//             if (Mathf.Approximately(sizeVec.z, 0f))
//             {
//                 sizeVec.z = 0.00001f;
//             }

//             xSize = sizeVec.x;
//             ySize = sizeVec.y;
//             zSize = sizeVec.z;

//             // 4. Center = (min + max) / 2:
//             Vector3 center = (min + max) * 0.5f;

//             bounds = new Bounds(center, sizeVec);
//         }

//         /// <summary>
//         /// Uniformly scales the TMP_Text’s transform so that its height (relative to externalTransform) becomes 'height'.
//         /// Returns the scaled Transform and its new localScale; also outputs the new local X-size.
//         /// </summary>
//         public static void FitToHeightLocalToExternalTransform(
//             this TMP_Text t,
//             Transform externalTransform,
//             float height,
//             out TMP_Text tmpTextOut,
//             out float newLocalXSize
//         )
//         {
//             tmpTextOut = t;
//             // Get current size relative to externalTransform:
//             Vector3 sizeBefore;
//             Vector3 minBefore, maxBefore;
//             float xSizeBefore, ySizeBefore, zSizeBefore;

//             t.GetSizeRelativeToExternalTransform(
//                 externalTransform,
//                 out sizeBefore,
//                 out minBefore,
//                 out maxBefore,
//                 out xSizeBefore,
//                 out ySizeBefore,
//                 out zSizeBefore
//             );

//             // Compute uniform scale factor so that new height = 'height':
//             float scaleFactor = height / sizeBefore.y;

//             // Apply uniform scale:
//             var tOut = t.transform;
//             t.transform.UniformScaleByFactor(scaleFactor, out tOut);

//             // Recompute size (and newLocalXSize) after scaling:
//             Vector3 sizeAfter;
//             Vector3 minAfter, maxAfter;
//             float xSizeAfter, ySizeAfter, zSizeAfter;

//             t.GetSizeRelativeToExternalTransform(
//                 externalTransform,
//                 out sizeAfter,
//                 out minAfter,
//                 out maxAfter,
//                 out xSizeAfter,
//                 out ySizeAfter,
//                 out zSizeAfter
//             );

//             newLocalXSize = xSizeAfter;
//         }

//         /// <summary>
//         /// Computes axis-aligned size, min, max of the TMP_Text rectangle relative to externalTransform.
//         /// Outputs Vector3 size (width, height, depth), the min & max points, and also xSize, ySize, zSize as floats.
//         /// </summary>
//         public static void GetSizeRelativeToExternalTransform(
//             this TMP_Text t,
//             Transform externalTransform,
//             out Vector3 size,
//             out Vector3 min,
//             out Vector3 max,
//             out float xSize,
//             out float ySize,
//             out float zSize
//         )
//         {
//             // 1. Get world-space corners:
//             t.GetWorldCorners(
//                 out Vector3 worldBL,
//                 out Vector3 worldTL,
//                 out Vector3 worldTR,
//                 out Vector3 worldBR
//             );

//             // 2. Convert each corner into externalTransform’s local space:
//             Vector3 blLocal = externalTransform.InverseTransformPoint(worldBL);
//             Vector3 tlLocal = externalTransform.InverseTransformPoint(worldTL);
//             Vector3 trLocal = externalTransform.InverseTransformPoint(worldTR);
//             Vector3 brLocal = externalTransform.InverseTransformPoint(worldBR);

//             // 3. Compute axis-aligned min & max:
//             float minX = Mathf.Min(blLocal.x, tlLocal.x, trLocal.x, brLocal.x);
//             float maxX = Mathf.Max(blLocal.x, tlLocal.x, trLocal.x, brLocal.x);
//             float minY = Mathf.Min(blLocal.y, tlLocal.y, trLocal.y, brLocal.y);
//             float maxY = Mathf.Max(blLocal.y, tlLocal.y, trLocal.y, brLocal.y);
//             float minZ = Mathf.Min(blLocal.z, tlLocal.z, trLocal.z, brLocal.z);
//             float maxZ = Mathf.Max(blLocal.z, tlLocal.z, trLocal.z, brLocal.z);

//             min = new Vector3(minX, minY, minZ);
//             max = new Vector3(maxX, maxY, maxZ);

//             // 4. Size = max - min:
//             size = new Vector3(
//                 maxX - minX,
//                 maxY - minY,
//                 maxZ - minZ
//             );

//             xSize = size.x;
//             ySize = size.y;
//             zSize = size.z;
//         }


//         /// <summary>
//         /// Uniformly scales a Transform by 'factor', and returns the same transform along with its new localScale.
//         /// </summary>

//     }
// }
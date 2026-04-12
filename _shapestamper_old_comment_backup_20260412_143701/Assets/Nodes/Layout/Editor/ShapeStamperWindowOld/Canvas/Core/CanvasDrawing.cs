// #if UNITY_EDITOR
// using UnityEditor;
// using UnityEngine;

// namespace DLN.EditorTools.ShapeStamper
// {
//     public static class CanvasDrawing
//     {
//         private const float PointRadius = 6f;
//         private const float HoveredPointRadius = 7f;
//         private const float SelectedPointRadius = 8f;
//         private const float OffsetHandleRadius = 5f;

//         public static void DrawDocument(
//             Rect canvasRect,
//             ICanvasDocument document,
//             CanvasSelection selection,
//             CanvasInteractionState interaction,
//             CanvasViewState view)
//         {
//             if (document == null)
//                 return;

//             Handles.BeginGUI();

//             DrawEdges(canvasRect, document, selection, interaction, view);
//             DrawOffsets(canvasRect, document, selection, interaction, view);
//             DrawPoints(canvasRect, document, selection, interaction, view);

//             Handles.EndGUI();
//         }

//         private static void DrawEdges(
//             Rect canvasRect,
//             ICanvasDocument document,
//             CanvasSelection selection,
//             CanvasInteractionState interaction,
//             CanvasViewState view)
//         {
//             foreach (var edge in document.Edges)
//             {
//                 if (!TryGetEdgeScreenPositions(edge, document, canvasRect, view, out Vector2 a, out Vector2 b))
//                     continue;

//                 bool isHovered = interaction.Hovered == CanvasElementRef.ForEdge(edge.Id);
//                 bool isSelected = selection.Contains(CanvasElementRef.ForEdge(edge.Id));

//                 Color color = Color.white;
//                 float thickness = 2f;

//                 if (isSelected)
//                 {
//                     color = Color.yellow;
//                     thickness = 3f;
//                 }
//                 else if (isHovered)
//                 {
//                     color = new Color(1f, 0.9f, 0.4f);
//                     thickness = 3f;
//                 }

//                 Color old = Handles.color;
//                 Handles.color = color;
//                 Handles.DrawAAPolyLine(thickness, a, b);
//                 Handles.color = old;
//             }
//         }

//         private static void DrawPoints(
//             Rect canvasRect,
//             ICanvasDocument document,
//             CanvasSelection selection,
//             CanvasInteractionState interaction,
//             CanvasViewState view)
//         {
//             foreach (var point in document.Points)
//             {
//                 Vector2 screenPos = CanvasMath.CanvasToScreen(point.Position, canvasRect, view, document);

//                 bool isHovered = interaction.Hovered == CanvasElementRef.ForPoint(point.Id);
//                 bool isSelected = selection.Contains(CanvasElementRef.ForPoint(point.Id));

//                 float radius = PointRadius;
//                 Color fill = new Color(0.3f, 0.9f, 0.3f);
//                 Color outline = Color.black;

//                 if (isSelected)
//                 {
//                     radius = SelectedPointRadius;
//                     fill = Color.green;
//                     outline = Color.yellow;
//                 }
//                 else if (isHovered)
//                 {
//                     radius = HoveredPointRadius;
//                     fill = new Color(0.5f, 1f, 0.5f);
//                     outline = Color.white;
//                 }

//                 DrawSolidDisc(screenPos, radius, fill);
//                 DrawWireDisc(screenPos, radius + 1f, outline);
//             }
//         }

//         private static void DrawOffsets(
//             Rect canvasRect,
//             ICanvasDocument document,
//             CanvasSelection selection,
//             CanvasInteractionState interaction,
//             CanvasViewState view)
//         {
//             foreach (var offset in document.Offsets)
//             {
//                 if (!CanvasMath.TryGetOffsetHandleScreenPosition(offset, document, canvasRect, view, out Vector2 handlePos))
//                     continue;

//                 bool isHovered = interaction.Hovered == CanvasElementRef.ForOffset(offset.Id);
//                 bool isSelected = selection.Contains(CanvasElementRef.ForOffset(offset.Id));

//                 float radius = OffsetHandleRadius;
//                 Color fill = new Color(0.4f, 0.8f, 1f);
//                 Color outline = Color.black;

//                 if (isSelected)
//                 {
//                     radius += 1f;
//                     fill = new Color(0.2f, 0.7f, 1f);
//                     outline = Color.yellow;
//                 }
//                 else if (isHovered)
//                 {
//                     radius += 1f;
//                     fill = new Color(0.6f, 0.9f, 1f);
//                     outline = Color.white;
//                 }

//                 DrawSolidDisc(handlePos, radius, fill);
//                 DrawWireDisc(handlePos, radius + 1f, outline);
//             }
//         }

//         private static void DrawSolidDisc(Vector2 center, float radius, Color color)
//         {
//             Color old = Handles.color;
//             Handles.color = color;
//             Handles.DrawSolidDisc(center, Vector3.forward, radius);
//             Handles.color = old;
//         }

//         private static void DrawWireDisc(Vector2 center, float radius, Color color)
//         {
//             Color old = Handles.color;
//             Handles.color = color;
//             Handles.DrawWireDisc(center, Vector3.forward, radius);
//             Handles.color = old;
//         }

//         private static bool TryGetEdgeScreenPositions(
//             CanvasEdge edge,
//             ICanvasDocument document,
//             Rect canvasRect,
//             CanvasViewState view,
//             out Vector2 a,
//             out Vector2 b)
//         {
//             a = default;
//             b = default;

//             if (!TryGetPoint(document, edge.A, out var pointA))
//                 return false;

//             if (!TryGetPoint(document, edge.B, out var pointB))
//                 return false;

//             a = CanvasMath.CanvasToScreen(pointA.Position, canvasRect, view, document);
//             b = CanvasMath.CanvasToScreen(pointB.Position, canvasRect, view, document);
//             return true;
//         }

//         private static bool TryGetPoint(ICanvasDocument document, int pointId, out CanvasPoint point)
//         {
//             foreach (var p in document.Points)
//             {
//                 if (p.Id == pointId)
//                 {
//                     point = p;
//                     return true;
//                 }
//             }

//             point = default;
//             return false;
//         }
//     }
// }
// #endif
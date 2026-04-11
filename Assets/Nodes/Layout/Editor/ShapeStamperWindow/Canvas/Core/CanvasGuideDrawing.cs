#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    /// <summary>
    /// Edge-language overlay drawing.
    ///
    /// Current intent:
    /// - Shape canvas: draw content edge on the frame, padding just inside, border just outside.
    ///   These are display lanes for now until the shape document stores explicit border/padding widths.
    /// - Profile canvas: draw top/bottom content/padding/border as HORIZONTAL lines using real document values.
    ///   Draw -Z / +Z content/padding/border as VERTICAL lines using a max-span mock display based on the
    ///   document's existing border/padding values.
    /// </summary>
    public static class CanvasGuideDrawing
    {
        private static readonly Color ContentColor = new Color(1f, 1f, 1f, 0.22f);
        private static readonly Color PaddingColor = new Color(0.35f, 0.85f, 1f, 0.30f);
        private static readonly Color BorderColor = new Color(1f, 0.65f, 0.20f, 0.30f);

        private const float ShapePaddingLanePixels = 18f;
        private const float ShapeBorderLanePixels = 18f;
        private const float LabelStep = 16f;

        public static void DrawShapeGuides(EditorCanvas canvas, Rect canvasRect, ShapeCanvasDocument document)
        {
            if (canvas == null || document == null)
                return;

            Vector2 topLeft = canvas.CanvasToScreen(new Vector2(0f, 0f));
            Vector2 topRight = canvas.CanvasToScreen(new Vector2(document.WorldSizeMeters.x, 0f));
            Vector2 bottomLeft = canvas.CanvasToScreen(new Vector2(0f, document.WorldSizeMeters.y));
            Vector2 bottomRight = canvas.CanvasToScreen(new Vector2(document.WorldSizeMeters.x, document.WorldSizeMeters.y));

            float leftX = topLeft.x;
            float rightX = topRight.x;
            float topY = topLeft.y;
            float bottomY = bottomLeft.y;

            // Content edges = actual frame edge.
            DrawLine(new Vector2(leftX, topY), new Vector2(leftX, bottomY), ContentColor);
            DrawLine(new Vector2(rightX, topY), new Vector2(rightX, bottomY), ContentColor);
            DrawLine(new Vector2(leftX, topY), new Vector2(rightX, topY), ContentColor);
            DrawLine(new Vector2(leftX, bottomY), new Vector2(rightX, bottomY), ContentColor);

            // Padding edges = inside the frame.
            DrawLine(
                new Vector2(leftX + ShapePaddingLanePixels, topY),
                new Vector2(leftX + ShapePaddingLanePixels, bottomY),
                PaddingColor);

            DrawLine(
                new Vector2(rightX - ShapePaddingLanePixels, topY),
                new Vector2(rightX - ShapePaddingLanePixels, bottomY),
                PaddingColor);

            DrawLine(
                new Vector2(leftX, topY + ShapePaddingLanePixels),
                new Vector2(rightX, topY + ShapePaddingLanePixels),
                PaddingColor);

            DrawLine(
                new Vector2(leftX, bottomY - ShapePaddingLanePixels),
                new Vector2(rightX, bottomY - ShapePaddingLanePixels),
                PaddingColor);

            // Border edges = outside the frame.
            DrawLine(
                new Vector2(leftX - ShapeBorderLanePixels, topY),
                new Vector2(leftX - ShapeBorderLanePixels, bottomY),
                BorderColor);

            DrawLine(
                new Vector2(rightX + ShapeBorderLanePixels, topY),
                new Vector2(rightX + ShapeBorderLanePixels, bottomY),
                BorderColor);

            DrawLine(
                new Vector2(leftX, topY - ShapeBorderLanePixels),
                new Vector2(rightX, topY - ShapeBorderLanePixels),
                BorderColor);

            DrawLine(
                new Vector2(leftX, bottomY + ShapeBorderLanePixels),
                new Vector2(rightX, bottomY + ShapeBorderLanePixels),
                BorderColor);

            // Labels
            DrawStackedLabel(
                new Vector2(leftX - ShapeBorderLanePixels - 6f, topY + 8f),
                TextAnchor.UpperRight,
                ("-X Border", BorderColor),
                ("-X Content", ContentColor),
                ("-X Padding", PaddingColor));

            DrawStackedLabel(
                new Vector2(rightX + ShapeBorderLanePixels + 6f, topY + 8f),
                TextAnchor.UpperLeft,
                ("+X Border", BorderColor),
                ("+X Content", ContentColor),
                ("+X Padding", PaddingColor));

            DrawStackedLabel(
                new Vector2(leftX + 88f, topY - ShapeBorderLanePixels - 4f),
                TextAnchor.LowerLeft,
                ("-Y Border", BorderColor),
                ("-Y Content", ContentColor),
                ("-Y Padding", PaddingColor));

            DrawStackedLabel(
                new Vector2(leftX + 88f, bottomY + ShapeBorderLanePixels + 4f),
                TextAnchor.UpperLeft,
                ("+Y Border", BorderColor),
                ("+Y Content", ContentColor),
                ("+Y Padding", PaddingColor));
        }

        public static void DrawProfileGuides(EditorCanvas canvas, Rect canvasRect, ProfileCanvasDocument document)
        {
            if (canvas == null || document == null)
                return;

            DrawProfileHorizontalGuides(canvas, canvasRect, document);
            DrawProfileVerticalGuides(canvas, canvasRect, document);
        }

        private static void DrawProfileHorizontalGuides(EditorCanvas canvas, Rect canvasRect, ProfileCanvasDocument document)
        {
            float width = document.WorldSizeMeters.x;
            float height = document.WorldSizeMeters.y;

            float topContentY = 0f;
            float topPaddingY = Mathf.Clamp(document.TopPadding, 0f, height);
            float topBorderY = Mathf.Clamp(document.TopPadding + document.TopBorder, 0f, height);

            float bottomContentY = height;
            float bottomPaddingY = Mathf.Clamp(height - document.BottomPadding, 0f, height);
            float bottomBorderY = Mathf.Clamp(height - (document.BottomPadding + document.BottomBorder), 0f, height);

            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, topContentY, ContentColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, topPaddingY, PaddingColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, topBorderY, BorderColor);

            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, bottomContentY, ContentColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, bottomPaddingY, PaddingColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, bottomBorderY, BorderColor);

            Vector2 topAnchor = canvas.CanvasToScreen(new Vector2(0f, topContentY));
            Vector2 bottomAnchor = canvas.CanvasToScreen(new Vector2(0f, bottomContentY));

            DrawStackedLabel(
                new Vector2(topAnchor.x + 8f, topAnchor.y + 8f),
                TextAnchor.UpperLeft,
                ("-Y Content", ContentColor),
                ("-Y Padding", PaddingColor),
                ("-Y Border", BorderColor));

            DrawStackedLabel(
                new Vector2(bottomAnchor.x + 8f, bottomAnchor.y - 8f),
                TextAnchor.LowerLeft,
                ("+Y Content", ContentColor),
                ("+Y Padding", PaddingColor),
                ("+Y Border", BorderColor));
        }

        private static void DrawProfileVerticalGuides(EditorCanvas canvas, Rect canvasRect, ProfileCanvasDocument document)
        {
            float width = document.WorldSizeMeters.x;
            float height = document.WorldSizeMeters.y;

            // Mock/display span:
            // show the largest lateral/depth span so users can reason about the edge family
            // before per-edge interpolation is fully wired in.
            float maxPaddingSpan = Mathf.Max(
                document.LeftPadding,
                document.RightPadding,
                document.TopPadding,
                document.BottomPadding,
                document.FrontPaddingDepth);

            float maxBorderOnly = Mathf.Max(
                document.LeftBorder,
                document.RightBorder,
                document.TopBorder,
                document.BottomBorder,
                document.FrontBorderDepth);

            float maxBorderSpan = maxPaddingSpan + maxBorderOnly;

            maxPaddingSpan = Mathf.Clamp(maxPaddingSpan, 0f, width * 0.5f);
            maxBorderSpan = Mathf.Clamp(maxBorderSpan, 0f, width * 0.5f);

            float negZContentX = 0f;
            float negZPaddingX = maxPaddingSpan;
            float negZBorderX = maxBorderSpan;

            float posZContentX = width;
            float posZPaddingX = width - maxPaddingSpan;
            float posZBorderX = width - maxBorderSpan;

            DrawVerticalWorldLine(canvas, canvasRect, negZContentX, 0f, height, ContentColor);
            DrawVerticalWorldLine(canvas, canvasRect, negZPaddingX, 0f, height, PaddingColor);
            DrawVerticalWorldLine(canvas, canvasRect, negZBorderX, 0f, height, BorderColor);

            DrawVerticalWorldLine(canvas, canvasRect, posZContentX, 0f, height, ContentColor);
            DrawVerticalWorldLine(canvas, canvasRect, posZPaddingX, 0f, height, PaddingColor);
            DrawVerticalWorldLine(canvas, canvasRect, posZBorderX, 0f, height, BorderColor);

            Vector2 negTop = canvas.CanvasToScreen(new Vector2(negZContentX, 0f));
            Vector2 posTop = canvas.CanvasToScreen(new Vector2(posZContentX, 0f));

            DrawStackedLabel(
                new Vector2(negTop.x + 6f, negTop.y + 56f),
                TextAnchor.UpperLeft,
                ("-Z Content", ContentColor),
                ("-Z Padding", PaddingColor),
                ("-Z Border", BorderColor));

            DrawStackedLabel(
                new Vector2(posTop.x - 6f, posTop.y + 56f),
                TextAnchor.UpperRight,
                ("+Z Content", ContentColor),
                ("+Z Padding", PaddingColor),
                ("+Z Border", BorderColor));
        }

        private static void DrawHorizontalWorldLine(
            EditorCanvas canvas,
            Rect canvasRect,
            float xMin,
            float xMax,
            float y,
            Color color)
        {
            Vector2 a = CanvasMath.CanvasToScreen(
                new Vector2(xMin, y),
                canvasRect,
                canvas.View,
                GetDocumentForCanvas(canvas));

            Vector2 b = CanvasMath.CanvasToScreen(
                new Vector2(xMax, y),
                canvasRect,
                canvas.View,
                GetDocumentForCanvas(canvas));

            DrawLine(a, b, color);
        }

        private static void DrawVerticalWorldLine(
            EditorCanvas canvas,
            Rect canvasRect,
            float x,
            float yMin,
            float yMax,
            Color color)
        {
            Vector2 a = CanvasMath.CanvasToScreen(
                new Vector2(x, yMin),
                canvasRect,
                canvas.View,
                GetDocumentForCanvas(canvas));

            Vector2 b = CanvasMath.CanvasToScreen(
                new Vector2(x, yMax),
                canvasRect,
                canvas.View,
                GetDocumentForCanvas(canvas));

            DrawLine(a, b, color);
        }

        private static ICanvasDocument GetDocumentForCanvas(EditorCanvas canvas)
        {
            return canvas.Document;
        }

        private static void DrawLine(Vector2 a, Vector2 b, Color color)
        {
            Handles.BeginGUI();
            Color old = Handles.color;
            Handles.color = color;
            Handles.DrawAAPolyLine(1.5f, a, b);
            Handles.color = old;
            Handles.EndGUI();
        }

        private static void DrawStackedLabel(
            Vector2 position,
            TextAnchor anchor,
            params (string text, Color color)[] lines)
        {
            float y = position.y;

            for (int i = 0; i < lines.Length; i++)
            {
                DrawSingleLabel(new Vector2(position.x, y), lines[i].text, lines[i].color, anchor);

                bool growsUp =
                    anchor == TextAnchor.LowerLeft ||
                    anchor == TextAnchor.LowerCenter ||
                    anchor == TextAnchor.LowerRight;

                y += growsUp ? -LabelStep : LabelStep;
            }
        }

        private static void DrawSingleLabel(Vector2 position, string text, Color color, TextAnchor anchor)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = anchor
            };

            style.normal.textColor = color;

            Vector2 size = style.CalcSize(new GUIContent(text));
            Rect rect = new Rect(position.x, position.y, size.x + 4f, size.y + 2f);

            switch (anchor)
            {
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    rect.x -= rect.width;
                    break;
            }

            switch (anchor)
            {
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    rect.y -= rect.height;
                    break;
            }

            GUI.Label(rect, text, style);
        }
    }
}
#endif
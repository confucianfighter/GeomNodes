#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public static class CanvasGuideDrawing
    {
        private static readonly Color ContentColor = new Color(1f, 1f, 1f, 0.22f);
        private static readonly Color PaddingColor = new Color(0.35f, 0.85f, 1f, 0.30f);
        private static readonly Color BorderColor = new Color(1f, 0.65f, 0.20f, 0.30f);

        public static void DrawShapeGuides(EditorCanvas canvas, Rect canvasRect, ShapeCanvasDocument document)
        {
            if (canvas == null || document == null)
                return;

            float width = document.WorldSizeMeters.x;
            float height = document.WorldSizeMeters.y;

            float leftContent = 0f;
            float leftPadding = Mathf.Clamp(document.LeftPadding, 0f, width);
            float leftBorder = -(document.LeftPadding + document.LeftBorder);

            float rightContent = width;
            float rightPadding = Mathf.Clamp(width - document.RightPadding, 0f, width);
            float rightBorder = width + document.RightPadding + document.RightBorder;

            float topContent = 0f;
            float topPadding = Mathf.Clamp(document.TopPadding, 0f, height);
            float topBorder = -(document.TopPadding + document.TopBorder);

            float bottomContent = height;
            float bottomPadding = Mathf.Clamp(height - document.BottomPadding, 0f, height);
            float bottomBorder = height + document.BottomPadding + document.BottomBorder;

            DrawVerticalWorldLine(canvas, canvasRect, leftBorder, 0f, height, BorderColor);
            DrawVerticalWorldLine(canvas, canvasRect, leftContent, 0f, height, ContentColor);
            DrawVerticalWorldLine(canvas, canvasRect, leftPadding, 0f, height, PaddingColor);

            DrawVerticalWorldLine(canvas, canvasRect, rightBorder, 0f, height, BorderColor);
            DrawVerticalWorldLine(canvas, canvasRect, rightContent, 0f, height, ContentColor);
            DrawVerticalWorldLine(canvas, canvasRect, rightPadding, 0f, height, PaddingColor);

            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, topBorder, BorderColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, topContent, ContentColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, topPadding, PaddingColor);

            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, bottomBorder, BorderColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, bottomContent, ContentColor);
            DrawHorizontalWorldLine(canvas, canvasRect, 0f, width, bottomPadding, PaddingColor);

            DrawVerticalWorldLineLabel(canvas, canvasRect, leftBorder, 0f, "-X Border", BorderColor, placeRight: false);
            DrawVerticalWorldLineLabel(canvas, canvasRect, leftContent, 0f, "-X Content", ContentColor, placeRight: true);
            DrawVerticalWorldLineLabel(canvas, canvasRect, leftPadding, 0f, "-X Padding", PaddingColor, placeRight: true);

            DrawVerticalWorldLineLabel(canvas, canvasRect, rightBorder, 0f, "+X Border", BorderColor, placeRight: true);
            DrawVerticalWorldLineLabel(canvas, canvasRect, rightContent, 0f, "+X Content", ContentColor, placeRight: false);
            DrawVerticalWorldLineLabel(canvas, canvasRect, rightPadding, 0f, "+X Padding", PaddingColor, placeRight: false);

            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, topBorder, "-Y Border", BorderColor, placeBelow: false);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, topContent, "-Y Content", ContentColor, placeBelow: true);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, topPadding, "-Y Padding", PaddingColor, placeBelow: true);

            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, bottomBorder, "+Y Border", BorderColor, placeBelow: true);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, bottomContent, "+Y Content", ContentColor, placeBelow: false);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, bottomPadding, "+Y Padding", PaddingColor, placeBelow: false);
        }

        public static void DrawProfileGuides(EditorCanvas canvas, Rect canvasRect, ProfileCanvasDocument document)
        {
            if (canvas == null || document == null)
                return;

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

            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, topContentY, "-Y Content", ContentColor, placeBelow: true);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, topPaddingY, "-Y Padding", PaddingColor, placeBelow: true);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, topBorderY, "-Y Border", BorderColor, placeBelow: false);

            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, bottomContentY, "+Y Content", ContentColor, placeBelow: false);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, bottomPaddingY, "+Y Padding", PaddingColor, placeBelow: false);
            DrawHorizontalWorldLineLabel(canvas, canvasRect, 0f, bottomBorderY, "+Y Border", BorderColor, placeBelow: true);

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

            DrawVerticalWorldLineLabel(canvas, canvasRect, negZContentX, 0f, "-Z Content", ContentColor, placeRight: true);
            DrawVerticalWorldLineLabel(canvas, canvasRect, negZPaddingX, 0f, "-Z Padding", PaddingColor, placeRight: true);
            DrawVerticalWorldLineLabel(canvas, canvasRect, negZBorderX, 0f, "-Z Border", BorderColor, placeRight: true);

            DrawVerticalWorldLineLabel(canvas, canvasRect, posZContentX, 0f, "+Z Content", ContentColor, placeRight: false);
            DrawVerticalWorldLineLabel(canvas, canvasRect, posZPaddingX, 0f, "+Z Padding", PaddingColor, placeRight: false);
            DrawVerticalWorldLineLabel(canvas, canvasRect, posZBorderX, 0f, "+Z Border", BorderColor, placeRight: false);
        }

        private static void DrawHorizontalWorldLine(
            EditorCanvas canvas,
            Rect canvasRect,
            float xMin,
            float xMax,
            float y,
            Color color)
        {
            Vector2 a = CanvasMath.CanvasToScreen(new Vector2(xMin, y), canvasRect, canvas.View, canvas.Document);
            Vector2 b = CanvasMath.CanvasToScreen(new Vector2(xMax, y), canvasRect, canvas.View, canvas.Document);
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
            Vector2 a = CanvasMath.CanvasToScreen(new Vector2(x, yMin), canvasRect, canvas.View, canvas.Document);
            Vector2 b = CanvasMath.CanvasToScreen(new Vector2(x, yMax), canvasRect, canvas.View, canvas.Document);
            DrawLine(a, b, color);
        }

        private static void DrawVerticalWorldLineLabel(
            EditorCanvas canvas,
            Rect canvasRect,
            float x,
            float y,
            string text,
            Color color,
            bool placeRight)
        {
            Vector2 top = CanvasMath.CanvasToScreen(new Vector2(x, y), canvasRect, canvas.View, canvas.Document);
            DrawVerticalLineLabel(top, text, color, placeRight);
        }

        private static void DrawHorizontalWorldLineLabel(
            EditorCanvas canvas,
            Rect canvasRect,
            float x,
            float y,
            string text,
            Color color,
            bool placeBelow)
        {
            Vector2 start = CanvasMath.CanvasToScreen(new Vector2(x, y), canvasRect, canvas.View, canvas.Document);
            DrawHorizontalLineLabel(start, text, color, placeBelow);
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

        private static void DrawVerticalLineLabel(Vector2 lineTop, string text, Color color, bool placeRight, float yOffset = 6f)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel);
            style.normal.textColor = color;
            style.alignment = placeRight ? TextAnchor.UpperLeft : TextAnchor.UpperRight;

            Vector2 size = style.CalcSize(new GUIContent(text));
            float x = placeRight ? lineTop.x + 4f : lineTop.x - 4f;
            Rect rect = new Rect(x, lineTop.y + yOffset, size.x + 4f, size.y + 2f);

            if (!placeRight)
                rect.x -= rect.width;

            GUI.Label(rect, text, style);
        }

        private static void DrawHorizontalLineLabel(Vector2 lineStart, string text, Color color, bool placeBelow, float xOffset = 6f)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel);
            style.normal.textColor = color;
            style.alignment = TextAnchor.MiddleLeft;

            Vector2 size = style.CalcSize(new GUIContent(text));
            float y = placeBelow ? lineStart.y + 2f : lineStart.y - size.y - 2f;
            Rect rect = new Rect(lineStart.x + xOffset, y, size.x + 4f, size.y + 2f);
            GUI.Label(rect, text, style);
        }
    }
}
#endif

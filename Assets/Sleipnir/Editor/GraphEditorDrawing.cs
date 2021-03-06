﻿using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Sleipnir.Editor
{
    public partial class GraphEditor
    {
        protected override void DrawEditor(int index)
        {
            if (_graph == null)
                return;

            var matrix = GUI.matrix;
            ProcessInput();
            DrawGrid();
            BeginZoomed();

            foreach (var connection in _graph.Connections())
                DrawConnection(connection);

            DrawConnectionToMouse();
            base.DrawEditor(index);

            EndZoomed();
            GUI.matrix = matrix;
        }

        #region Grid
        private void DrawGrid()
        {
            if (_gridTexture == null || _crossTexture == null)
                return;

            var windowRect = new Rect(Vector2.zero, position.size);
            var center = windowRect.size * 0.5f;

            // Offset from origin in tile units
            var xOffset = -(center.x * Scale + Position.x)
                / _gridTexture.width;
            var yOffset = ((center.y - windowRect.size.y) * Scale + Position.y)
                / _gridTexture.height;

            var tileOffset = new Vector2(xOffset, yOffset);

            // Amount of tiles
            var tileAmountX = Mathf.Round(windowRect.size.x * Scale)
                / _gridTexture.width;
            var tileAmountY = Mathf.Round(windowRect.size.y * Scale)
                / _gridTexture.height;

            var tileAmount = new Vector2(tileAmountX, tileAmountY);

            // Draw tiled background
            GUI.DrawTextureWithTexCoords(windowRect, _gridTexture,
                new Rect(tileOffset, tileAmount));
            GUI.DrawTextureWithTexCoords(windowRect, _crossTexture,
                new Rect(tileOffset + new Vector2(0.5f, 0.5f), tileAmount));
        }
        #endregion

        #region Connections
        private void DrawConnectionToMouse()
        {
            if (_selectedKnob == null)
                return;

            var knobPosition = GridToGuiPositionNoClip(_selectedKnob.Rect.center);
            var mousePosition = Event.current.mousePosition;

            if (_selectedKnob.Type == KnobType.Output)
                DrawConnection(knobPosition, mousePosition, _selectedKnob.Color);
            else
                DrawConnection(mousePosition, knobPosition, _selectedKnob.Color);
        }

        private static Color ConnectionColor(Knob outputKnob, Knob inputKnob)
        {
            return outputKnob.Color == inputKnob.Color
                ? outputKnob.Color
                : Color.Lerp(inputKnob.Color, outputKnob.Color, 0.5f);
        }

        private static void DrawConnection(Vector2 startGridPosition, Vector2 endGridPosition,
            Color color, float width = ConnectionLineWidth)
        {
            var distance = endGridPosition - startGridPosition;
            var angle = Mathf.Atan2(distance.y, distance.x) / 4;
            var outputVector = new Vector2(1, Mathf.Sin(angle));
            var inputVector = Mathf.Abs(Mathf.Atan2(distance.y, distance.x)) * Mathf.Rad2Deg > 150 
                ? new Vector2(-1, Mathf.Sin(angle)) 
                : new Vector2(-1, -Mathf.Sin(angle));

            Handles.DrawBezier(
                startGridPosition,
                endGridPosition,
                startGridPosition + outputVector * distance.magnitude,
                endGridPosition + inputVector * distance.magnitude,
                color,
                null,
                width);
        }

        private bool IsPointInBezierRange(Vector2 point, Connection connection)
        {
            var startGridPosition = GridToGuiPositionNoClip(connection.OutputKnob.Rect.center);
            var endGridPosition = GridToGuiPositionNoClip(connection.InputKnob.Rect.center);

            var distance = endGridPosition - startGridPosition;
            var angle = Mathf.Atan2(distance.y, distance.x) / 4;
            var outputVector = new Vector2(1, Mathf.Sin(angle));
            var inputVector = Mathf.Abs(Mathf.Atan2(distance.y, distance.x)) * Mathf.Rad2Deg > 150
                ? new Vector2(-1, Mathf.Sin(angle))
                : new Vector2(-1, -Mathf.Sin(angle));

            return HandleUtility.DistancePointBezier(
                       GridToGuiPositionNoClip(point),
                       startGridPosition,
                       endGridPosition,
                       startGridPosition + outputVector * distance.magnitude,
                       endGridPosition + inputVector * distance.magnitude) < ConnectionLineWidth;
        }

        private void DrawConnection(Connection connection)
        {
            var start = GridToGuiPositionNoClip(connection.OutputKnob.Rect.center);
            var end = GridToGuiPositionNoClip(connection.InputKnob.Rect.center);
            DrawConnection(start, end, ConnectionColor(connection.OutputKnob, connection.InputKnob));
        }
        #endregion

        #region Knobs
        // y axis is inverted
        public void Draw(Knob knob, EditorNode editorNode)
        {
            var rect = GetKnobRect(knob, editorNode);
            knob.Rect = rect;

            GUIHelper.PushColor(knob.Color);
            if (GUI.Button(GridToGuiDrawRect(rect), ""))
                OnKnobClick(knob);
            GUIHelper.PopColor();

            // knob label
            if (knob.Description.IsNullOrWhitespace())
                return;

            var style = _knobLabelGUIStyle;
            var labelContent = new GUIContent(knob.Description);
            var labelSize = style.CalcSize(labelContent);

            var labelPosition = knob.Type == KnobType.Input
                ? rect.position + new Vector2(-labelSize.x - KnobLabelOffset, 0)
                : rect.position + new Vector2(KnobSize.x + KnobLabelOffset, 0);

            var labelRect = new Rect(labelPosition, labelSize);
            GUI.Label(GridToGuiDrawRect(labelRect), labelContent, style);
        }
        
        private static Rect GetKnobRect(Knob knob, EditorNode editorNode)
        {
            var yPosition = editorNode.Content.HeaderRect.position.y + knob.RelativeYPosition;
            var xPosition = knob.Type == KnobType.Input
                ? editorNode.Content.HeaderRect.position.x - KnobSize.x - KnobHorizontalOffset
                : editorNode.Content.HeaderRect.position.x + editorNode.Content.HeaderRect.width + KnobHorizontalOffset;
            return new Rect(xPosition, yPosition, KnobSize.x, KnobSize.y);
        }
        #endregion
    }
}
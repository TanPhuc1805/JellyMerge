using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(JellyPiece))]
public class JellyPieceEditor : Editor
{
    private static readonly Color GRID_COLOR = new Color(0.3f, 0.3f, 0.3f);
    private static readonly Color CELL_BORDER_COLOR = new Color(0.2f, 0.2f, 0.2f);
    private const float CELL_SIZE = 50f;
    private const float CELL_SPACING = 4f;
    
    // Color palette (lấy từ JellyPiece)
    private static readonly Dictionary<JellyColor, Color> ColorPalette = new Dictionary<JellyColor, Color>
    {
        { JellyColor.None, new Color(0.15f, 0.15f, 0.15f) },
        { JellyColor.Red, new Color(1f, 0.2f, 0.2f) },
        { JellyColor.Blue, new Color(0.2f, 0.5f, 1f) },
        { JellyColor.Green, new Color(0.2f, 1f, 0.2f) },
        { JellyColor.Yellow, new Color(1f, 0.9f, 0.2f) },
        { JellyColor.Purple, new Color(0.8f, 0.2f, 1f) },
        { JellyColor.Pink, new Color(1f, 0.4f, 0.8f) },
        { JellyColor.Cyan, new Color(0.2f, 1f, 1f) },
        { JellyColor.Orange, new Color(1f, 0.6f, 0.2f) }
    };

    public override void OnInspectorGUI()
    {
        JellyPiece piece = (JellyPiece)target;
        
        serializedObject.Update();

        // Header
        EditorGUILayout.Space(5);
        DrawHeader();
        EditorGUILayout.Space(10);
        
        // Visual Grid Editor
        DrawVisualGrid(piece);
        EditorGUILayout.Space(10);
        
        // Color Palette
        DrawColorPalette(piece);
        EditorGUILayout.Space(10);
        
        // Hiển thị các trường mới
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("Prefab Setup", EditorStyles.boldLabel);

        // Hiển thị 2 trường còn lại
        EditorGUILayout.PropertyField(serializedObject.FindProperty("jellyCellPrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("visualsParent"));

        // --- THAY ĐỔI: Đã XÓA dòng 'visualCellSize' ---
        // Biến này không còn tồn tại trên JellyPiece.cs (vì ta dùng scale/pos cố định)
        // EditorGUILayout.PropertyField(serializedObject.FindProperty("visualCellSize"));
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        
        EditorGUILayout.LabelField("🎨 Jelly Piece Designer", titleStyle);
        EditorGUILayout.LabelField("Click cells to change colors", 
            EditorStyles.centeredGreyMiniLabel);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawVisualGrid(JellyPiece piece)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        Rect gridRect = GUILayoutUtility.GetRect(
            CELL_SIZE * 2 + CELL_SPACING * 3, 
            CELL_SIZE * 2 + CELL_SPACING * 3
        );
        
        float startX = gridRect.x + (gridRect.width - (CELL_SIZE * 2 + CELL_SPACING * 3)) / 2;
        float startY = gridRect.y + CELL_SPACING;
        
        EditorGUI.DrawRect(
            new Rect(startX, startY, CELL_SIZE * 2 + CELL_SPACING * 3, CELL_SIZE * 2 + CELL_SPACING * 3),
            GRID_COLOR
        );
        
        // Draw cells
        DrawCell(serializedObject.FindProperty("topLeft"), startX + CELL_SPACING, startY + CELL_SPACING, "TL");
        DrawCell(serializedObject.FindProperty("topRight"), startX + CELL_SIZE + CELL_SPACING * 2, startY + CELL_SPACING, "TR");
        DrawCell(serializedObject.FindProperty("bottomLeft"), startX + CELL_SPACING, startY + CELL_SIZE + CELL_SPACING * 2, "BL");
        DrawCell(serializedObject.FindProperty("bottomRight"), startX + CELL_SIZE + CELL_SPACING * 2, startY + CELL_SIZE + CELL_SPACING * 2, "BR");
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawCell(SerializedProperty colorProperty, float x, float y, string label)
    {
        JellyColor cellColor = (JellyColor)colorProperty.enumValueIndex;
        
        Rect cellRect = new Rect(x, y, CELL_SIZE, CELL_SIZE);
        
        Color color = ColorPalette[cellColor];
        EditorGUI.DrawRect(cellRect, color);
        DrawRectOutline(cellRect, CELL_BORDER_COLOR, 2f);
        
        if (cellColor != JellyColor.None)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 9,
                normal = { textColor = new Color(1, 1, 1, 0.5f) }
            };
            GUI.Label(new Rect(x + 4, y + 2, CELL_SIZE, 20), label, labelStyle);
            
            GUIStyle nameStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = GetContrastColor(color) }
            };
            GUI.Label(new Rect(x, y + CELL_SIZE / 2 - 8, CELL_SIZE, 16), cellColor.ToString(), nameStyle);
        }
        else
        {
            GUIStyle emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 20,
                normal = { textColor = new Color(1, 1, 1, 0.1f) }
            };
            GUI.Label(cellRect, "∅", emptyStyle);
        }
        
        Event e = Event.current;
        if (e.type == EventType.MouseDown && cellRect.Contains(e.mousePosition))
        {
            // Cycle color
            int currentIndex = colorProperty.enumValueIndex;
            currentIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(JellyColor)).Length;
            colorProperty.enumValueIndex = currentIndex;
            
            e.Use();
        }
    }
    
    private void DrawColorPalette(JellyPiece piece)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎨 Quick Color Palette", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        int colorCount = 0;
        foreach (var kvp in ColorPalette)
        {
            if (kvp.Key == JellyColor.None) continue;
            
            Rect colorRect = GUILayoutUtility.GetRect(30, 30, GUILayout.Width(30), GUILayout.Height(30));
            EditorGUI.DrawRect(colorRect, kvp.Value);
            DrawRectOutline(colorRect, Color.black, 1f);
            
            colorCount++;
            if (colorCount % 4 == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }
    
    // --- Helper methods (Giữ nguyên) ---
    
    private Color GetContrastColor(Color bgColor)
    {
        float luminance = 0.299f * bgColor.r + 0.587f * bgColor.g + 0.114f * bgColor.b;
        return luminance > 0.5f ? Color.black : Color.white;
    }
    
    private void DrawRectOutline(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }
}
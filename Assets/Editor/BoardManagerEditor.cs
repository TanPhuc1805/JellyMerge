using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(BoardManager))]
public class BoardManagerEditor : Editor
{
    private BoardManager boardManager;
    private SerializedProperty sp_gridWidth;
    private SerializedProperty sp_gridHeight;
    private SerializedProperty sp_levelLayout;

    private Vector2Int selectedPieceCoord = new Vector2Int(-1, -1);

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

    private void OnEnable()
    {
        boardManager = (BoardManager)target;
        sp_gridWidth = serializedObject.FindProperty("gridWidth");
        sp_gridHeight = serializedObject.FindProperty("gridHeight");
        sp_levelLayout = serializedObject.FindProperty("levelLayout");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(sp_gridWidth);
        EditorGUILayout.PropertyField(sp_gridHeight);
        EditorGUILayout.Space();

        DrawPropertiesExcluding(serializedObject, "m_Script", "gridWidth", "gridHeight", "levelLayout");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("Level Layout Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Click the grid below to design the map:\n" +
            "• Left Click: Toggle cell on/off (Shape the map)\n" +
            "• Right Click: Add / Remove initial piece (P)\n\n" +
            "Click a 'P' cell to select it and edit its colors below.",
            MessageType.Info);
        EditorGUILayout.Space(5);

        bool sizeChanged = CheckAndResizeLayout();

        DrawLayoutGrid();

        DrawSelectedPieceEditor();

        if (GUI.changed || sizeChanged)
        {
            EditorUtility.SetDirty(boardManager);
        }
        serializedObject.ApplyModifiedProperties();
    }

    private bool CheckAndResizeLayout()
    {
        int width = sp_gridWidth.intValue;
        int height = sp_gridHeight.intValue;
        if (width <= 0 || height <= 0) return false;

        int expectedCount = width * height;

        if (sp_levelLayout.arraySize != expectedCount)
        {
            Dictionary<Vector2Int, BoardManager.BoardCellData> oldData = new Dictionary<Vector2Int, BoardManager.BoardCellData>();
            foreach (var cell in boardManager.levelLayout)
            {
                if (cell != null)
                    oldData[cell.position] = cell;
            }
            
            boardManager.levelLayout.Clear();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int currentPos = new Vector2Int(x, y);
                    BoardManager.BoardCellData cell;
                    if (!oldData.TryGetValue(currentPos, out cell))
                    {
                        cell = new BoardManager.BoardCellData { position = currentPos, isEnabled = true, hasInitialPiece = false };
                    }
                    boardManager.levelLayout.Add(cell);
                }
            }
            serializedObject.Update();
            return true;
        }
        return false;
    }

    private void DrawLayoutGrid()
    {
        int width = sp_gridWidth.intValue;
        int height = sp_gridHeight.intValue;
        if (width <= 0 || height <= 0) return;
        
        float cellSize = 30f;
        float spacing = 2f;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        Rect gridRect = GUILayoutUtility.GetRect(
            (cellSize + spacing) * width + spacing,
            (cellSize + spacing) * height + spacing
        );

        GUI.Box(gridRect, "");

        for (int y_inv = 0; y_inv < height; y_inv++)
        {
            int y = height - 1 - y_inv;
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index >= sp_levelLayout.arraySize) continue;

                SerializedProperty cellProp = sp_levelLayout.GetArrayElementAtIndex(index);
                SerializedProperty sp_isEnabled = cellProp.FindPropertyRelative("isEnabled");
                SerializedProperty sp_hasInitialPiece = cellProp.FindPropertyRelative("hasInitialPiece");

                Rect cellRect = new Rect(
                    gridRect.x + spacing + x * (cellSize + spacing),
                    gridRect.y + spacing + y_inv * (cellSize + spacing),
                    cellSize,
                    cellSize
                );

                string cellLabel = "";
                Color cellColor;

                if (!sp_isEnabled.boolValue)
                {
                    cellColor = new Color(0.2f, 0.2f, 0.2f);
                    cellLabel = "X";
                }
                else if (sp_hasInitialPiece.boolValue)
                {
                    cellColor = new Color(0.2f, 0.5f, 1f);
                    cellLabel = "P";
                    
                    if(selectedPieceCoord.x == x && selectedPieceCoord.y == y)
                    {
                        cellColor = Color.cyan;
                    }
                }
                else
                {
                    cellColor = new Color(0.5f, 0.5f, 0.5f);
                    cellLabel = "";
                }

                GUI.backgroundColor = cellColor;
                GUI.Box(cellRect, cellLabel, EditorStyles.miniButton);

                Event e = Event.current;
                if (e.type == EventType.MouseDown && cellRect.Contains(e.mousePosition))
                {
                    if (e.button == 0)
                    {
                        sp_isEnabled.boolValue = !sp_isEnabled.boolValue;
                        if (!sp_isEnabled.boolValue)
                        {
                            sp_hasInitialPiece.boolValue = false;
                        }
                    }
                    else if (e.button == 1)
                    {
                        if (sp_isEnabled.boolValue)
                        {
                            sp_hasInitialPiece.boolValue = !sp_hasInitialPiece.boolValue;
                        }
                    }

                    if (sp_hasInitialPiece.boolValue)
                    {
                        selectedPieceCoord = new Vector2Int(x, y);
                    }
                    else if (selectedPieceCoord.x == x && selectedPieceCoord.y == y)
                    {
                        selectedPieceCoord = new Vector2Int(-1, -1);
                    }

                    e.Use();
                }
            }
        }
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = Color.white;
    }

    private void DrawSelectedPieceEditor()
    {
        if (selectedPieceCoord.x < 0 || selectedPieceCoord.y < 0)
        {
            return;
        }

        int index = selectedPieceCoord.y * sp_gridWidth.intValue + selectedPieceCoord.x;
        if (index >= sp_levelLayout.arraySize)
        {
            selectedPieceCoord = new Vector2Int(-1, -1);
            return;
        }

        SerializedProperty cellProp = sp_levelLayout.GetArrayElementAtIndex(index);

        if (!cellProp.FindPropertyRelative("hasInitialPiece").boolValue)
        {
            selectedPieceCoord = new Vector2Int(-1, -1);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"Editing Initial Piece at ({selectedPieceCoord.x}, {selectedPieceCoord.y})", EditorStyles.boldLabel);

        SerializedProperty sp_tl = cellProp.FindPropertyRelative("initial_TL");
        SerializedProperty sp_tr = cellProp.FindPropertyRelative("initial_TR");
        SerializedProperty sp_bl = cellProp.FindPropertyRelative("initial_BL");
        SerializedProperty sp_br = cellProp.FindPropertyRelative("initial_BR");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        float cellSize = 50f;
        float spacing = 4f;
        Rect gridRect = GUILayoutUtility.GetRect(
            cellSize * 2 + spacing * 3, 
            cellSize * 2 + spacing * 3
        );
        
        float startX = gridRect.x + (gridRect.width - (cellSize * 2 + spacing * 3)) / 2;
        float startY = gridRect.y + spacing;

        DrawEditorCell(sp_tl, startX + spacing, startY + spacing, "TL");
        DrawEditorCell(sp_tr, startX + cellSize + spacing * 2, startY + spacing, "TR");
        DrawEditorCell(sp_bl, startX + spacing, startY + cellSize + spacing * 2, "BL");
        DrawEditorCell(sp_br, startX + cellSize + spacing * 2, startY + cellSize + spacing * 2, "BR");

        EditorGUILayout.EndVertical();
    }

    private void DrawEditorCell(SerializedProperty colorProperty, float x, float y, string label)
    {
        JellyColor cellColor = (JellyColor)colorProperty.enumValueIndex;
        Rect cellRect = new Rect(x, y, 50, 50);
        
        Color color = ColorPalette[cellColor];
        EditorGUI.DrawRect(cellRect, color);
        
        if (cellColor != JellyColor.None)
        {
            GUIStyle nameStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = (0.299f * color.r + 0.587f * color.g + 0.114f * color.b) > 0.5f ? Color.black : Color.white }
            };
            GUI.Label(cellRect, cellColor.ToString(), nameStyle);
        }
        else
        {
             GUI.Label(cellRect, "∅", EditorStyles.centeredGreyMiniLabel);
        }
        
        Event e = Event.current;
        if (e.type == EventType.MouseDown && cellRect.Contains(e.mousePosition) && e.button == 0)
        {
            int currentIndex = colorProperty.enumValueIndex;
            currentIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(JellyColor)).Length;
            colorProperty.enumValueIndex = currentIndex;
            e.Use();
        }
    }
}
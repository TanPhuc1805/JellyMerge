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

    // Lưu lại ô piece đang được chọn để sửa màu
    private Vector2Int selectedPieceCoord = new Vector2Int(-1, -1);

    // (Copy từ JellyPieceEditor.cs để dùng cho việc hiển thị)
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

    /// <summary>
    /// (HÀM ĐÃ ĐƯỢC VIẾT LẠI)
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // --- PHẦN 1: VẼ CÁC TRƯỜNG CẤU HÌNH ---
        
        // Vẽ 2 trường Width và Height
        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(sp_gridWidth);
        EditorGUILayout.PropertyField(sp_gridHeight);
        EditorGUILayout.Space();

        // (THAY ĐỔI QUAN TRỌNG)
        // Vẽ TẤT CẢ các trường public khác (cellSize, prefabs, timeBetweenCombos)
        // NGOẠI TRỪ các trường ta sẽ vẽ thủ công bên dưới.
        DrawPropertiesExcluding(serializedObject, "m_Script", "gridWidth", "gridHeight", "levelLayout");

        
        // --- PHẦN 2: VẼ MAP EDITOR TÙY CHỈNH ---
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("Level Layout Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Click chuột vào lưới bên dưới để thiết kế map:\n" +
            "• Chuột Trái: Bật / Tắt ô (Tạo hình dạng map)\n" +
            "• Chuột Phải: Thêm / Xóa Piece đặt sẵn (chữ P)\n\n" +
            "Click vào ô 'P' để chọn và sửa màu cho piece đó ở bên dưới.",
            MessageType.Info);
        EditorGUILayout.Space(5);

        // Kiểm tra nếu size thay đổi thì tự động cập nhật list
        bool sizeChanged = CheckAndResizeLayout();

        // Vẽ lưới layout
        DrawLayoutGrid();

        // Vẽ bảng editor 2x2 cho piece đang được chọn
        DrawSelectedPieceEditor();

        if (GUI.changed || sizeChanged)
        {
            EditorUtility.SetDirty(boardManager);
        }
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// (HÀM NÀY ĐÃ BỊ XÓA)
    /// Chúng ta không cần hàm 'DrawDefaultInspectorWithoutScript()' nữa
    /// vì đã dùng 'DrawPropertiesExcluding()'
    /// </summary>
    // private void DrawDefaultInspectorWithoutScript() { ... }


    /// <summary>
    /// Tự động điều chỉnh List 'levelLayout' khi 'gridWidth' hoặc 'gridHeight' thay đổi
    /// </summary>
    private bool CheckAndResizeLayout()
    {
        int width = sp_gridWidth.intValue;
        int height = sp_gridHeight.intValue;
        if (width <= 0 || height <= 0) return false;

        int expectedCount = width * height;

        if (sp_levelLayout.arraySize != expectedCount)
        {
            // Dùng Dictionary để lưu data cũ
            Dictionary<Vector2Int, BoardManager.BoardCellData> oldData = new Dictionary<Vector2Int, BoardManager.BoardCellData>();
            foreach (var cell in boardManager.levelLayout)
            {
                if (cell != null)
                    oldData[cell.position] = cell;
            }
            
            boardManager.levelLayout.Clear(); // Xóa list cũ

            // Tạo list mới, điền data cũ vào nếu có
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int currentPos = new Vector2Int(x, y);
                    BoardManager.BoardCellData cell;
                    if (!oldData.TryGetValue(currentPos, out cell))
                    {
                        // Đây là ô mới, dùng giá trị mặc định
                        cell = new BoardManager.BoardCellData { position = currentPos, isEnabled = true, hasInitialPiece = false };
                    }
                    boardManager.levelLayout.Add(cell);
                }
            }
            serializedObject.Update(); // Báo cho SerializedObject biết là target đã thay đổi
            return true;
        }
        return false;
    }

    /// <summary>
    /// Vẽ lưới layout 2D trong Inspector
    /// </summary>
    private void DrawLayoutGrid()
    {
        int width = sp_gridWidth.intValue;
        int height = sp_gridHeight.intValue;
        if (width <= 0 || height <= 0) return;
        
        float cellSize = 30f;
        float spacing = 2f;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        // Lấy 1 Rect đủ lớn cho cả lưới
        Rect gridRect = GUILayoutUtility.GetRect(
            (cellSize + spacing) * width + spacing,
            (cellSize + spacing) * height + spacing
        );

        GUI.Box(gridRect, ""); // Vẽ nền cho lưới

        for (int y_inv = 0; y_inv < height; y_inv++)
        {
            int y = height - 1 - y_inv; // Vẽ từ trên xuống (y=0 ở dưới)
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index >= sp_levelLayout.arraySize) continue; // An toàn

                SerializedProperty cellProp = sp_levelLayout.GetArrayElementAtIndex(index);
                SerializedProperty sp_isEnabled = cellProp.FindPropertyRelative("isEnabled");
                SerializedProperty sp_hasInitialPiece = cellProp.FindPropertyRelative("hasInitialPiece");

                Rect cellRect = new Rect(
                    gridRect.x + spacing + x * (cellSize + spacing),
                    gridRect.y + spacing + y_inv * (cellSize + spacing), // y_inv để vẽ từ trên xuống
                    cellSize,
                    cellSize
                );

                // Quyết định màu và chữ
                string cellLabel = "";
                Color cellColor;

                if (!sp_isEnabled.boolValue)
                {
                    cellColor = new Color(0.2f, 0.2f, 0.2f); // Màu ô bị tắt
                    cellLabel = "X";
                }
                else if (sp_hasInitialPiece.boolValue)
                {
                    cellColor = new Color(0.2f, 0.5f, 1f); // Màu xanh cho "Piece"
                    cellLabel = "P";
                    
                    // Highlight nếu đang được chọn
                    if(selectedPieceCoord.x == x && selectedPieceCoord.y == y)
                    {
                        cellColor = Color.cyan;
                    }
                }
                else
                {
                    cellColor = new Color(0.5f, 0.5f, 0.5f); // Màu ô trống (spot)
                    cellLabel = "";
                }

                GUI.backgroundColor = cellColor;
                GUI.Box(cellRect, cellLabel, EditorStyles.miniButton);

                // Xử lý click chuột
                Event e = Event.current;
                if (e.type == EventType.MouseDown && cellRect.Contains(e.mousePosition))
                {
                    if (e.button == 0) // Chuột trái -> Bật/Tắt ô
                    {
                        sp_isEnabled.boolValue = !sp_isEnabled.boolValue;
                        if (!sp_isEnabled.boolValue)
                        {
                            sp_hasInitialPiece.boolValue = false; // Tắt luôn piece nếu tắt ô
                        }
                    }
                    else if (e.button == 1) // Chuột phải -> Bật/Tắt piece
                    {
                        if (sp_isEnabled.boolValue) // Chỉ cho phép trên ô đang bật
                        {
                            sp_hasInitialPiece.boolValue = !sp_hasInitialPiece.boolValue;
                        }
                    }

                    // Nếu click vào ô piece, chọn nó
                    if (sp_hasInitialPiece.boolValue)
                    {
                        selectedPieceCoord = new Vector2Int(x, y);
                    }
                    else if (selectedPieceCoord.x == x && selectedPieceCoord.y == y)
                    {
                        selectedPieceCoord = new Vector2Int(-1, -1); // Bỏ chọn
                    }

                    e.Use(); // Đánh dấu sự kiện đã được xử lý
                }
            }
        }
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = Color.white; // Reset màu
    }

    /// <summary>
    /// Vẽ 4 ô màu 2x2 để chỉnh piece
    /// </summary>
    private void DrawSelectedPieceEditor()
    {
        if (selectedPieceCoord.x < 0 || selectedPieceCoord.y < 0)
        {
            return; // Không có piece nào được chọn
        }

        int index = selectedPieceCoord.y * sp_gridWidth.intValue + selectedPieceCoord.x;
        if (index >= sp_levelLayout.arraySize)
        {
            selectedPieceCoord = new Vector2Int(-1, -1); // Lỗi, bỏ chọn
            return;
        }

        SerializedProperty cellProp = sp_levelLayout.GetArrayElementAtIndex(index);

        // Nếu ô này không còn piece nữa, bỏ chọn
        if (!cellProp.FindPropertyRelative("hasInitialPiece").boolValue)
        {
            selectedPieceCoord = new Vector2Int(-1, -1);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"Editing Initial Piece at ({selectedPieceCoord.x}, {selectedPieceCoord.y})", EditorStyles.boldLabel);

        // Lấy 4 thuộc tính màu
        SerializedProperty sp_tl = cellProp.FindPropertyRelative("initial_TL");
        SerializedProperty sp_tr = cellProp.FindPropertyRelative("initial_TR");
        SerializedProperty sp_bl = cellProp.FindPropertyRelative("initial_BL");
        SerializedProperty sp_br = cellProp.FindPropertyRelative("initial_BR");

        // Vẽ 4 ô (copy từ JellyPieceEditor)
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

    /// <summary>
    /// Helper: Vẽ 1 ô màu có thể click để đổi màu
    /// </summary>
    private void DrawEditorCell(SerializedProperty colorProperty, float x, float y, string label)
    {
        JellyColor cellColor = (JellyColor)colorProperty.enumValueIndex;
        Rect cellRect = new Rect(x, y, 50, 50);
        
        Color color = ColorPalette[cellColor];
        EditorGUI.DrawRect(cellRect, color);
        
        // Vẽ chữ tên màu
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
        
        // Xử lý click (chuột trái)
        Event e = Event.current;
        if (e.type == EventType.MouseDown && cellRect.Contains(e.mousePosition) && e.button == 0)
        {
            // Cycle color
            int currentIndex = colorProperty.enumValueIndex;
            currentIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(JellyColor)).Length;
            colorProperty.enumValueIndex = currentIndex;
            e.Use();
        }
    }
}
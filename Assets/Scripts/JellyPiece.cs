using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Struct để JellyPiece báo cáo kết quả cho BoardManager
/// </summary>
public struct PieceUpdateResult
{
    public bool wasModified;
    public bool wasDestroyed;
}

public class JellyPiece : MonoBehaviour
{
    #region Configuration
    [Header("Visuals")]
    [SerializeField] private GameObject jellyCellPrefab;
    [SerializeField] private Transform visualsParent;

    [Header("🎨 Visual Designer")]
    [Tooltip("Top-Left cell (0,1)")]
    public JellyColor topLeft = JellyColor.None;
    [Tooltip("Top-Right cell (1,1)")]
    public JellyColor topRight = JellyColor.None;
    [Tooltip("Bottom-Left cell (0,0)")]
    public JellyColor bottomLeft = JellyColor.None;
    [Tooltip("Bottom-Right cell (1,0)")]
    public JellyColor bottomRight = JellyColor.None;
    #endregion

    #region Constants
    private const float CELL_SCALE_1X = 0.5f; 
    private const float CELL_SCALE_2X = 1.0f;
    private const float CELL_POS_HALF = 0.25f;
    private const float CELL_POS_ZERO = 0.0f;
    #endregion

    #region State
    private JellyColor[,] colorGrid = new JellyColor[2, 2];
    private List<GameObject> activeVisuals = new List<GameObject>();
    [HideInInspector] 
    public Vector2Int baseBoardPosition; 
    private const int LAYER_JELLY = 6; 
    #endregion

    //-------------------------------------------------
    #region Initialization
    //-------------------------------------------------
    
    void Awake()
    {
        gameObject.layer = LAYER_JELLY;
        InitializeFromInspector();
    }
    
    public void InitializeFromInspector()
    {
        colorGrid = new JellyColor[2, 2];
        colorGrid[0, 0] = bottomLeft;
        colorGrid[1, 0] = bottomRight;
        colorGrid[0, 1] = topLeft;
        colorGrid[1, 1] = topRight;
        UpdateVisuals(false);
    }

    public void Initialize(JellyColor[,] newGrid)
    {
        colorGrid = newGrid;
        bottomLeft = colorGrid[0, 0];
        bottomRight = colorGrid[1, 0];
        topLeft = colorGrid[0, 1];
        topRight = colorGrid[1, 1];
        UpdateVisuals(false); 
    }
    #endregion

    //-------------------------------------------------
    #region Public API (2-Phase Logic)
    //-------------------------------------------------

    public JellyColor GetColorAt(int localX, int localY)
    {
        if (localX < 0 || localX > 1 || localY < 0 || localY > 1)
        {
            return JellyColor.None;
        }
        return colorGrid[localX, localY];
    }

    /// <summary>
    /// GIAI ĐOẠN 1: Tính Toán (Phase Xóa / Logic Scale)
    /// 
    /// ⭐ THAY ĐỔI CHÍNH:
    /// - XÓA: Toàn bộ enum ShapeCase (8 cases phức tạp)
    /// - XÓA: Hàm AnalyzeCurrentShape() (phân tích hình dạng trước khi xóa)
    /// - THÊM: Logic đơn giản theo SỐ Ô CÒN LẠI (4 cases)
    /// </summary>
    public PieceUpdateResult ApplyLogicUpdate(List<Vector2Int> localCoordsToClear)
    {
        if (localCoordsToClear == null || localCoordsToClear.Count == 0)
        {
            return new PieceUpdateResult { wasModified = false, wasDestroyed = false };
        }

        // 1. Xóa các ô bị match
        foreach (var coord in localCoordsToClear)
        {
            if (colorGrid[coord.x, coord.y] != JellyColor.None)
            {
                colorGrid[coord.x, coord.y] = JellyColor.None;
            }
        }

        // 2. Đếm số ô còn lại (không phải None)
        List<Vector2Int> remainingCells = new List<Vector2Int>();
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (colorGrid[x, y] != JellyColor.None)
                {
                    remainingCells.Add(new Vector2Int(x, y));
                }
            }
        }

        int remainingCount = remainingCells.Count;

        // 3. Áp dụng logic scale theo 4 TH
        switch (remainingCount)
        {
            case 0: // TH4: Tất cả bị hủy → Phá vỡ piece
                return new PieceUpdateResult { wasModified = true, wasDestroyed = true };

            case 1: // TH3: 3 ô bị hủy, 1 ô còn lại → Scale thành 4 ô
                HandleCase_OneRemaining(remainingCells[0]);
                break;

            case 2: // TH2: 2 ô bị hủy, 2 ô còn lại
                HandleCase_TwoRemaining(remainingCells);
                break;

            case 3: // TH1: 1 ô bị hủy, 3 ô còn lại
                HandleCase_ThreeRemaining(remainingCells);
                break;
        }

        return new PieceUpdateResult { wasModified = true, wasDestroyed = false };
    }
    
    /// <summary>
    /// GIAI ĐOẠN 2: Thực Thi (Phase Scale)
    /// Chạy animation scale
    /// </summary>
    public void PlayVisualUpdate()
    {
        UpdateVisuals(true);
    }

    /// <summary>
    /// GIAI ĐOẠN 2: Thực Thi (Phase Hủy)
    /// Chạy animation hủy piece
    /// </summary>
    public void PlayDestroySequence()
    {
        SetAllCells(JellyColor.None);
        UpdateVisuals(true);
        Destroy(gameObject, 0.5f);
    }
    #endregion

    //-------------------------------------------------
    #region Scale Logic (4 Trường Hợp)
    //-------------------------------------------------

    /// <summary>
    /// TH1: 3 ô còn lại → Ưu tiên scale ngang, VÀ sửa lỗi 3A-1B L-shape
    /// </summary>
/// <summary>
    /// TH1: 3 ô còn lại → Tìm ô 1x1 (isolate) và scale nó vào ô trống.
    /// (Sửa lỗi scale nhầm màu của khối 1x2/2x1)
    /// </summary>
    private void HandleCase_ThreeRemaining(List<Vector2Int> remaining)
    {
        // 1. Tìm ô bị hủy (ô trống)
        Vector2Int cleared = FindClearedCell(remaining);
        
        // 2. Tìm "ô 1x1 còn lại" (isolate cell)
        // Ô isolate là ô không liền kề VÀ cùng màu
        // với bất kỳ ô nào khác trong 3 ô còn lại.
        
        Vector2Int isolateCell = Vector2Int.zero;

        // Lấy 3 ô còn lại
        Vector2Int c1 = remaining[0];
        Vector2Int c2 = remaining[1];
        Vector2Int c3 = remaining[2];

        // Dùng GetLinkedCells để tìm nhóm liên kết
        // GetLinkedCells sẽ trả về tất cả các ô cùng màu và liền kề
        List<Vector2Int> group1 = GetLinkedCells(c1.x, c1.y);
        
        // Lọc lại danh sách group1 để chỉ chứa các ô trong 'remaining'
        group1.RemoveAll(cell => !remaining.Contains(cell));

        // 3. Phân tích nhóm
        if (group1.Count == 1)
        {
            // c1 chính là ô 1x1 isolate
            isolateCell = c1;
        }
        else if (group1.Count == 2)
        {
            // c1 là một phần của khối 2-cell.
            // Ô isolate là ô không nằm trong nhóm này.
            isolateCell = remaining.Find(c => !group1.Contains(c));
        }
        else // group1.Count == 3
        {
            // Cả 3 ô đều liên kết (L-shape cùng màu).
            // Không có ô isolate. Chọn c1 làm "isolate" cũng không sao,
            // vì logic scale ngang/dọc của bạn trước đó cũng hợp lý.
            // Nhưng để nhất quán, chúng ta sẽ scale c1.
            isolateCell = c1; 
        }

        // 4. Lấy màu của ô isolate
        JellyColor isolateColor = colorGrid[isolateCell.x, isolateCell.y];

        // 5. Áp dụng logic:
        // "Màu của ô 1x1 còn lại scale vào chỗ trống"
        
        // Tô màu ô trống
        colorGrid[cleared.x, cleared.y] = isolateColor;
        
        // Tô màu (khẳng định lại màu) của ô isolate
        // để đảm bảo chúng tạo thành một khối 2-cell mới.
        colorGrid[isolateCell.x, isolateCell.y] = isolateColor;
    }
    

    /// <summary>
    /// TH2: 2 ô còn lại → Scale thành khối 2 ô (ưu tiên ngang) hoặc 4 ô
    /// </summary>
    private void HandleCase_TwoRemaining(List<Vector2Int> remaining)
    {
        Vector2Int cell1 = remaining[0];
        Vector2Int cell2 = remaining[1];
        
        JellyColor color1 = colorGrid[cell1.x, cell1.y];
        JellyColor color2 = colorGrid[cell2.x, cell2.y];
        
        bool isAdjacent = IsAdjacent(cell1, cell2);
        
        if (isAdjacent)
        {
            // --- TRƯỜNG HỢP A: 2 Ô LIỀN KỀ ---
            
            if(color1 == color2)
            {
                // 1. Cùng màu: "còn lại khối 2 ô thì scale thành khối 4 ô"
                SetAllCells(color1);
            }
            else
            {
                // 2. Khác màu: "còn lại 2 khối 1 ô thì scale thành khối 2 ô"
                // Chúng ta sẽ scale cả 2 ô ra để lấp đầy
                
                if (cell1.x == cell2.x) // Chúng đang ở CÙNG CỘT (dọc)
                {
                    // Scale chúng theo chiều ngang
                    colorGrid[1 - cell1.x, cell1.y] = color1; // Scale ô 1
                    colorGrid[1 - cell2.x, cell2.y] = color2; // Scale ô 2
                }
                else // Chúng đang ở CÙNG HÀNG (ngang)
                {
                    // Scale chúng theo chiều dọc
                    colorGrid[cell1.x, 1 - cell1.y] = color1; // Scale ô 1
                    colorGrid[cell2.x, 1 - cell2.y] = color2; // Scale ô 2
                }
            }
        }
        else
        {
            // --- TRƯỜNG HỢP B: 2 Ô CHÉO ---
            // "ưu tiên scale chiều ngang"
            
            // Scale ô 1 theo chiều ngang
            colorGrid[1 - cell1.x, cell1.y] = color1; 
            
            // Scale ô 2 theo chiều ngang
            colorGrid[1 - cell2.x, cell2.y] = color2;
        }
    }

    /// <summary>
    /// TH3: 1 ô còn lại → Scale thành khối 4 ô
    /// </summary>
    private void HandleCase_OneRemaining(Vector2Int remaining)
    {
        JellyColor newColor = colorGrid[remaining.x, remaining.y];
        SetAllCells(newColor);
    }

    #endregion

    //-------------------------------------------------
    #region Helper Functions
    //-------------------------------------------------

    /// <summary>
    /// Tìm ô bị hủy (ô không có trong danh sách remaining)
    /// </summary>
    private Vector2Int FindClearedCell(List<Vector2Int> remaining)
    {
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!remaining.Contains(cell))
                {
                    return cell;
                }
            }
        }
        return Vector2Int.zero; // Fallback
    }

    /// <summary>
    /// Kiểm tra 2 ô có liền kề không (ngang hoặc dọc)
    /// </summary>
    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        
        // Liền kề ngang hoặc dọc (không chéo)
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    private void SetAllCells(JellyColor color)
    {
        colorGrid[0, 0] = color;
        colorGrid[1, 0] = color;
        colorGrid[0, 1] = color;
        colorGrid[1, 1] = color;
    }

    // --- THÊM HÀM NÀY VÀO JELLYPIECE.CS ---

    /// <summary>
    /// (HÀM HELPER MỚI) Tìm tất cả các ô con liền kề cùng màu
    /// (Sử dụng BFS - Thuật toán loang)
    /// </summary>
    public List<Vector2Int> GetLinkedCells(int startX, int startY)
    {
        List<Vector2Int> linkedCells = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        bool[,] visited = new bool[2, 2];

        JellyColor targetColor = GetColorAt(startX, startY);
        if (targetColor == JellyColor.None)
        {
            linkedCells.Add(new Vector2Int(startX, startY));
            return linkedCells; // Chỉ trả về chính ô đó nếu nó là None
        }

        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        Vector2Int[] neighbors = { 
            new Vector2Int(0, 1), new Vector2Int(0, -1), 
            new Vector2Int(1, 0), new Vector2Int(-1, 0) 
        };

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();
            linkedCells.Add(pos);

            foreach (var neighborOffset in neighbors)
            {
                Vector2Int nPos = pos + neighborOffset;

                // Kiểm tra 4 hướng
                if (nPos.x >= 0 && nPos.x < 2 && nPos.y >= 0 && nPos.y < 2 &&
                    !visited[nPos.x, nPos.y] && 
                    colorGrid[nPos.x, nPos.y] == targetColor)
                {
                    visited[nPos.x, nPos.y] = true;
                    queue.Enqueue(nPos);
                }
            }
        }
        
        return linkedCells;
    }

    #endregion

    //-------------------------------------------------
    #region Visuals (Giữ Nguyên)
    //-------------------------------------------------

    private void UpdateVisuals(bool animate = false)
    {
        foreach (GameObject visual in activeVisuals)
        {
            if (Application.isPlaying) Destroy(visual);
            else DestroyImmediate(visual);
        }
        activeVisuals.Clear();
        
        if (jellyCellPrefab == null || visualsParent == null) return;

        bool[,] processed = new bool[2, 2];

        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 2; x++)
            {
                if (processed[x, y]) continue;
                JellyColor color = colorGrid[x, y];
                
                // Kiểm tra 2x2
                bool canMerge_2x2 = (color != JellyColor.None) && (x == 0 && y == 0) && (colorGrid[1, 0] == color) && !processed[1, 0] && (colorGrid[0, 1] == color) && !processed[0, 1] && (colorGrid[1, 1] == color) && !processed[1, 1];
                if (canMerge_2x2)
                {
                    SpawnVisual(color, new Vector3(CELL_POS_ZERO, CELL_POS_ZERO, 0), new Vector3(CELL_SCALE_2X, CELL_SCALE_2X, CELL_SCALE_1X), animate);
                    processed[0, 0] = true; processed[1, 0] = true; processed[0, 1] = true; processed[1, 1] = true;
                }
                // Kiểm tra 2x1 Ngang
                else if ((color != JellyColor.None) && x == 0 && (colorGrid[1, y] == color) && !processed[1, y]) 
                {
                    SpawnVisual(color, new Vector3(CELL_POS_ZERO, (y == 0) ? -CELL_POS_HALF : CELL_POS_HALF, 0), new Vector3(CELL_SCALE_2X, CELL_SCALE_1X, CELL_SCALE_1X), animate);
                    processed[0, y] = true; processed[1, y] = true;
                }
                // Kiểm tra 1x2 Dọc
                else if ((color != JellyColor.None) && y == 0 && (colorGrid[x, 1] == color) && !processed[x, 1]) 
                {
                    SpawnVisual(color, new Vector3((x == 0) ? -CELL_POS_HALF : CELL_POS_HALF, CELL_POS_ZERO, 0), new Vector3(CELL_SCALE_1X, CELL_SCALE_2X, CELL_SCALE_1X), animate);
                    processed[x, 0] = true; processed[x, 1] = true;
                }
                // Còn lại là 1x1
                else 
                {
                    SpawnVisual(color, new Vector3((x == 0) ? -CELL_POS_HALF : CELL_POS_HALF, (y == 0) ? -CELL_POS_HALF : CELL_POS_HALF, 0), new Vector3(CELL_SCALE_1X, CELL_SCALE_1X, CELL_SCALE_1X), animate);
                    processed[x, y] = true;
                }
            }
        }
    }

    private void SpawnVisual(JellyColor color, Vector3 localPos, Vector3 localScale, bool animate = false)
    {
        GameObject newCell = Instantiate(jellyCellPrefab, visualsParent);
        newCell.transform.localPosition = localPos;
        
        Renderer cellRenderer = newCell.GetComponentInChildren<Renderer>();
        if (cellRenderer != null)
        {
            cellRenderer.material.color = GetColorValue(color);
        }
        
        if (animate && Application.isPlaying)
        {
            if (color == JellyColor.None)
            {
                newCell.transform.localScale = localScale;
                newCell.transform.DOScale(Vector3.zero, 0.3f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => { if (newCell != null) Destroy(newCell); }); 
            }
            else
            {
                newCell.transform.localScale = localScale * 0.3f;
                newCell.transform.DOScale(localScale, 1f)
                    .SetEase(Ease.OutBack);
                activeVisuals.Add(newCell);
            }
        }
        else
        {
            newCell.transform.localScale = localScale;
            if (color != JellyColor.None)
            {
                activeVisuals.Add(newCell);
            }
            else
            {
                newCell.SetActive(false);
            }
        }
    }

    public static Color GetColorValue(JellyColor jellyColor)
    {
        switch (jellyColor)
        {
            case JellyColor.Red: return new Color(1f, 0.2f, 0.2f);
            case JellyColor.Blue: return new Color(0.2f, 0.5f, 1f);
            case JellyColor.Green: return new Color(0.2f, 1f, 0.2f);
            case JellyColor.Yellow: return new Color(1f, 0.9f, 0.2f);
            case JellyColor.Purple: return new Color(0.8f, 0.2f, 1f);
            case JellyColor.Pink: return new Color(1f, 0.4f, 0.8f);
            case JellyColor.Cyan: return new Color(0.2f, 1f, 1f);
            case JellyColor.Orange: return new Color(1f, 0.6f, 0.2f);
            default: return Color.white;
        }
    }
    #endregion
}

public enum JellyColor
{
    None,
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
    Pink,
    Cyan,
    Orange
}
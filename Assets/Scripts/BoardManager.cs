using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq; // Cần cho GroupBy, ToDictionary, Distinct

public class BoardManager : MonoBehaviour
{
    // (CLASS MỚI) Dùng để lưu trữ data của level layout
    [System.Serializable]
    public class BoardCellData
    {
        public Vector2Int position;
        public bool isEnabled = true; // Ô này có tồn tại không?
        public bool hasInitialPiece = false; // Ô này có piece đặt sẵn không?

        // 4 màu của piece đặt sẵn
        public JellyColor initial_TL = JellyColor.None;
        public JellyColor initial_TR = JellyColor.None;
        public JellyColor initial_BL = JellyColor.None;
        public JellyColor initial_BR = JellyColor.None;
    }


    #region Configuration & Prefabs
    [Header("Board Configuration")]
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 1f;

    [Tooltip("Khoảng cách padding (theo đơn vị Unity) để căn giữa bản đồ")]
    public float padding = 3f;

    [Header("Prefabs")]
    public GameObject jellySpotPrefab;
    public GameObject jellyPiecePrefab; // <-- (MỚI) Cần prefab của JellyPiece
    
    [Header("References")]
    [Tooltip("Tự động tìm nếu bỏ trống")]
    [SerializeField] private GameManager gameManager;

    [Header("Match Logic")]
    [Tooltip("Thời gian chờ (giây) sau khi một lượt match/scale kết thúc, trước khi quét tìm combo mới.")]
    [SerializeField] private float timeBetweenCombos = 0.5f;

    // --- Dữ Liệu Của Board ---
    private JellyPiece[,] pieceGrid;
    private JellySpot[,] jellySpotGrid;

    // (MỚI) Dữ liệu layout này sẽ được tùy chỉnh bởi Editor
    [HideInInspector]
    public List<BoardCellData> levelLayout = new List<BoardCellData>();

    // Biến trạng thái
    private bool isCheckingMatches = false;
    #endregion

    //-------------------------------------------------
    #region Board Initialization
    //-------------------------------------------------
    
    void Start()
    {
        // (SỬA ĐỔI) Tự động tìm GameManager
        if (gameManager == null)
        {
            gameManager = GameManager.Instance; // Ưu tiên Singleton
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>(); // Fallback
            }
        }
        
        CreateBoard();
        CenterMap();
    }

    /// <summary>
    /// (ĐÃ VIẾT LẠI) Tạo board dựa trên dữ liệu 'levelLayout'
    /// </summary>
    private void CreateBoard()
    {
        pieceGrid = new JellyPiece[gridWidth, gridHeight];
        jellySpotGrid = new JellySpot[gridWidth, gridHeight];

        // (MỚI) Dùng Dictionary để truy cập layout nhanh hơn
        Dictionary<Vector2Int, BoardCellData> layoutDict = new Dictionary<Vector2Int, BoardCellData>();
        foreach(var cell in levelLayout)
        {
            if (cell != null)
                layoutDict[cell.position] = cell;
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                BoardCellData cellData = null;
                
                // Lấy data của ô này, nếu không có (lần đầu chạy) thì dùng mặc định
                if (!layoutDict.TryGetValue(pos, out cellData))
                {
                    cellData = new BoardCellData { position = pos, isEnabled = true }; // Mặc định là bật
                }

                // A. Kiểm tra xem SPOT này có được BẬT hay không
                if (cellData.isEnabled)
                {
                    // 1. Tạo Spot
                    Vector3 position = new Vector3(x * cellSize, y * cellSize, 0);
                    GameObject spotObj = Instantiate(jellySpotPrefab, position, Quaternion.identity, transform);
                    spotObj.name = $"Spot_{x}_{y}";
                    // Dịch tâm spot cho đúng (tùy theo logic của bạn, giả sử prefab đã ở tâm)
                    spotObj.transform.position += new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0);

                    JellySpot spot = spotObj.GetComponent<JellySpot>();
                    if (spot != null)
                    {
                        spot.gridPosition = new Vector2Int(x, y);
                        jellySpotGrid[x, y] = spot;
                    }

                    // B. Kiểm tra xem có PIECE ĐẶT SẴN không
                    if (cellData.hasInitialPiece)
                    {
                        if (jellyPiecePrefab != null)
                        {
                            // 2. Tạo Piece
                            GameObject pieceObj = Instantiate(jellyPiecePrefab, spot.transform.position, Quaternion.identity);
                            JellyPiece piece = pieceObj.GetComponent<JellyPiece>();
                            
                            // 3. Cấu hình màu cho piece
                            JellyColor[,] colors = new JellyColor[2, 2];
                            colors[0, 1] = cellData.initial_TL;
                            colors[1, 1] = cellData.initial_TR;
                            colors[0, 0] = cellData.initial_BL;
                            colors[1, 0] = cellData.initial_BR;
                            piece.Initialize(colors);

                            // 4. Đặt piece lên board (không animation, không check match)
                            PlaceInitialPiece(piece, spot);
                        }
                        else
                        {
                            Debug.LogError("Muốn tạo piece ban đầu nhưng 'jellyPiecePrefab' chưa được gán trên BoardManager!", this);
                        }
                    }
                }
                // else: (cellData.isEnabled == false) -> Không làm gì cả, jellySpotGrid[x, y] sẽ là null
            }
        }
    }

    /// <summary>
    /// (HÀM MỚI) Đặt piece lên board lúc ban đầu
    /// </summary>
    private void PlaceInitialPiece(JellyPiece piece, JellySpot spot)
    {
        Vector2Int targetPos = spot.gridPosition;

        // 1. Cập nhật data của Board
        pieceGrid[targetPos.x, targetPos.y] = piece;
        spot.isOccupied = true;
        
        // 2. Lưu vị trí gốc vào piece
        piece.baseBoardPosition = targetPos;
        
        // 3. Snap vị trí VÀ ĐẶT LÀM CON CỦA SPOT
        piece.transform.SetParent(spot.transform);
        piece.transform.position = spot.transform.position; // Không dùng DOTween
        
        // 4. Vô hiệu hóa script kéo thả cho piece này
        PieceDragger dragger = piece.GetComponent<PieceDragger>();
        if(dragger != null)
        {
            // Đánh dấu là đã đặt và vô hiệu hóa
            dragger.SetIsPlaced(true);
            dragger.enabled = false; 
        }
        
        // KHÔNG KÍCH HOẠT QUÉT MATCH
    }
    #endregion

    //-------------------------------------------------
    #region Piece Placement (1x1 Logic)
    // (Logic này dùng khi NGƯỜI CHƠI kéo thả)
    //-------------------------------------------------

    /// <summary>
    /// Thử đặt piece vào 1 Spot cụ thể (được gọi bởi PieceDragger)
    /// </summary>
    public bool TryPlacePiece(JellyPiece piece, JellySpot spot)
    {
        // (SỬA ĐỔI) Kiểm tra trạng thái GameManager
        if (isCheckingMatches || spot == null || spot.isOccupied || 
            (gameManager != null && gameManager.CurrentState != GameManager.GameState.Playing))
        {
            return false;
        }
        
        Vector2Int targetPos = spot.gridPosition;

        // 1. Cập nhật data của Board
        pieceGrid[targetPos.x, targetPos.y] = piece;
        spot.isOccupied = true;
        
        // 2. Lưu vị trí gốc vào piece
        piece.baseBoardPosition = targetPos;
        
        // 3. Snap vị trí VÀ ĐẶT LÀM CON CỦA SPOT
        piece.transform.SetParent(spot.transform);
        piece.transform.DOMove(spot.transform.position, 0.2f).SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                // 4. Kích hoạt quét SAU KHI đã snap
                StartCoroutine(ProcessBoardMatches());
            });
        
        return true;
    }

    /// <summary>
    /// Ẩn TẤT CẢ các border (được gọi bởi PieceDragger)
    /// </summary>
    public void HideAllBorders()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (jellySpotGrid[x, y] != null)
                {
                    jellySpotGrid[x, y].HideBorder();
                }
            }
        }
    }
    #endregion

    //-------------------------------------------------
    #region Match & Combo Logic (ĐÃ CẬP NHẬT)
    //-------------------------------------------------

    // (Lưu trữ 1 lệnh hủy: Piece nào, ô local (x,y) nào)
    private struct ClearCommand
    {
        public JellyPiece piece;
        public int localX;
        public int localY;

        public ClearCommand(JellyPiece p, int x, int y)
        {
            piece = p; localX = x; localY = y;
        }
        
        public override bool Equals(object obj)
        {
            return obj is ClearCommand cmd &&
                   EqualityComparer<JellyPiece>.Default.Equals(piece, cmd.piece) &&
                   localX == cmd.localX &&
                   localY == cmd.localY;
        }
        public override int GetHashCode()
        {
            int hashCode = 17;
            hashCode = hashCode * 31 + (piece == null ? 0 : piece.GetHashCode());
            hashCode = hashCode * 31 + localX.GetHashCode();
            hashCode = hashCode * 31 + localY.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// (ĐÃ CẬP NHẬT) Coroutine chính xử lý 2 giai đoạn
    /// Tích hợp logic đếm (theo khối) và báo cáo cho GameManager
    /// </summary>
    private IEnumerator ProcessBoardMatches()
    {
        isCheckingMatches = true;

        while (true)
        {
            // 1. TÌM TẤT CẢ CÁC CẠNH MATCH
            List<ClearCommand> commands = FindAllEdgeMatches();
            if (commands.Count == 0)
            {
                break; // Không còn combo, dừng vòng lặp
            }

            // --- GIAI ĐOẠN 1: TÍNH TOÁN (PHASE XÓA / LOGIC SCALE) ---
            
            // 2. GOM CÁC LỆNH HỦY THEO TỪNG PIECE
            var commandsByPiece = commands.Distinct()
                .GroupBy(c => c.piece)
                .ToDictionary(
                    g => g.Key, 
                    g => g.Select(cmd => new Vector2Int(cmd.localX, cmd.localY)).ToList()
                );

            // 3. HỎI TỪNG PIECE XEM KẾT QUẢ SẼ THẾ NÀO
            var updateResults = new List<(JellyPiece, PieceUpdateResult)>();
            
            // (*** MỚI: Dictionary để đếm màu bị phá hủy ***)
            Dictionary<JellyColor, int> totalClearedThisTurn = new Dictionary<JellyColor, int>();

            foreach (var pair in commandsByPiece)
            {
                JellyPiece piece = pair.Key;
                List<Vector2Int> coordsToClear = pair.Value; // Danh sách các ô 1x1 bị match

                if (piece == null) continue; // Piece đã bị hủy ở vòng lặp combo trước

                // (*** LOGIC ĐẾM MỚI: Đếm theo khối (block) ***)
                // Dùng HashSet để theo dõi các ô 1x1 đã được đếm
                // (vì 1 khối 2x2 chỉ tính là 1)
                HashSet<Vector2Int> countedCellsOnThisPiece = new HashSet<Vector2Int>();

                foreach (var coord in coordsToClear)
                {
                    // Nếu ô này đã thuộc về 1 khối đã đếm -> bỏ qua
                    if (countedCellsOnThisPiece.Contains(coord))
                    {
                        continue;
                    }

                    // Lấy màu của ô này
                    JellyColor color = piece.GetColorAt(coord.x, coord.y);
                    if (color != JellyColor.None)
                    {
                        // --- ĐÂY LÀ MỘT KHỐI MỚI CHƯA ĐẾM ---
                        
                        // 1. Đếm khối này (tính là 1)
                        totalClearedThisTurn.TryGetValue(color, out int currentCount);
                        totalClearedThisTurn[color] = currentCount + 1;

                        // 2. Lấy TẤT CẢ các ô 1x1 thuộc khối này
                        List<Vector2Int> blockCells = piece.GetLinkedCells(coord.x, coord.y);
                        
                        // 3. Đánh dấu TẤT CẢ các ô đó là "đã đếm"
                        foreach (var cell in blockCells)
                        {
                            countedCellsOnThisPiece.Add(cell);
                        }
                    }
                }
                
                // (*** HẾT LOGIC ĐẾM MỚI ***)

                // Gọi hàm "Tính Toán"
                PieceUpdateResult result = piece.ApplyLogicUpdate(coordsToClear);
                
                // Lưu lại kết quả
                updateResults.Add((piece, result));
            }


            // (*** MỚI: Báo cáo cho GameManager ***)
            if (totalClearedThisTurn.Count > 0 && gameManager != null)
            {
                gameManager.ReportColorsCleared(totalClearedThisTurn);
            }

            // --- GIAI ĐOẠN 2: THỰC THI (PHASE SCALE / HỦY) ---

            // 4. RA LỆNH CHO PIECE CHẠY ANIMATION VÀ DỌN DẸP
            foreach (var (piece, result) in updateResults)
            {
                if (piece == null) continue; // An toàn
                
                if (result.wasDestroyed)
                {
                    // A. Piece bị hủy
                    piece.PlayDestroySequence(); // Chạy animation hủy
                    
                    // BoardManager CHỦ ĐỘNG dọn dẹp
                    Vector2Int pos = piece.baseBoardPosition;
                    if(IsValidPosition(pos)) // Kiểm tra an toàn
                    {
                        pieceGrid[pos.x, pos.y] = null;
                        if(jellySpotGrid[pos.x, pos.y] != null) // Spot có thể không tồn tại
                        {
                            jellySpotGrid[pos.x, pos.y].isOccupied = false;
                        }
                    }
                }
                else if (result.wasModified)
                {
                    // B. Piece bị scale
                    piece.PlayVisualUpdate(); // Chạy animation scale
                }
            }

            // 5. ĐỢI ANIMATION KẾT THÚC
            // (1s cho scale-up, 0.3s cho scale-down)
            yield return new WaitForSeconds(1.1f); 
            
            // 6. ĐỢI 1 CHÚT TRƯỚC KHI QUÉT LẠI
            yield return new WaitForSeconds(timeBetweenCombos);
        }

        isCheckingMatches = false;

        // (*** MỚI: Kiểm tra Game Over SAU KHI kết thúc tất cả combo ***)
        if (gameManager != null)
        {
            gameManager.CheckForGameOver();
        }
    }

    /// <summary>
    /// Quét board, so sánh các cạnh
    /// </summary>
    private List<ClearCommand> FindAllEdgeMatches()
    {
        List<ClearCommand> commands = new List<ClearCommand>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                JellyPiece pieceA = pieceGrid[x, y];
                if (pieceA == null) continue;

                // 1. So Sánh BÊN PHẢI (A-phải vs B-trái)
                if (x + 1 < gridWidth)
                {
                    JellyPiece pieceB = pieceGrid[x + 1, y];
                    if (pieceB != null)
                    {
                        CheckAndAdd(commands, pieceA, 1, 0, pieceB, 0, 0); // Hàng dưới
                        CheckAndAdd(commands, pieceA, 1, 1, pieceB, 0, 1); // Hàng trên
                    }
                }
                
                // 2. So Sánh BÊN TRÊN (A-trên vs C-dưới)
                if (y + 1 < gridHeight)
                {
                    JellyPiece pieceC = pieceGrid[x, y + 1];
                    if (pieceC != null)
                    {
                        CheckAndAdd(commands, pieceA, 0, 1, pieceC, 0, 0); // Cột trái
                        CheckAndAdd(commands, pieceA, 1, 1, pieceC, 1, 0); // Cột phải
                    }
                }
            }
        }
        return commands;
    }

    /// <summary>
    /// Helper: Kiểm tra 2 ô con, nếu match thì thêm TOÀN BỘ KHỐI vào danh sách
    /// </summary>
    private void CheckAndAdd(List<ClearCommand> list,
                             JellyPiece pieceA, int ax, int ay,
                             JellyPiece pieceB, int bx, int by)
    {
        JellyColor colorA = pieceA.GetColorAt(ax, ay);

        // Chỉ match nếu A có màu VÀ A match B
        if (colorA != JellyColor.None && colorA == pieceB.GetColorAt(bx, by))
        {
            // (LOGIC MỚI)
            // Lấy TOÀN BỘ khối liên kết từ PieceA
            List<Vector2Int> linkedCellsA = pieceA.GetLinkedCells(ax, ay);
            foreach (var cellPos in linkedCellsA)
            {
                list.Add(new ClearCommand(pieceA, cellPos.x, cellPos.y));
            }

            // Lấy TOÀN BỘ khối liên kết từ PieceB
            List<Vector2Int> linkedCellsB = pieceB.GetLinkedCells(bx, by);
            foreach (var cellPos in linkedCellsB)
            {
                list.Add(new ClearCommand(pieceB, cellPos.x, cellPos.y));
            }
        }
    }


    #endregion

    //-------------------------------------------------
    #region Helper Functions
    //-------------------------------------------------

    /// <summary>
    /// (HÀM MỚI) Tự động dịch chuyển BoardManager để
    /// căn giữa map vào gốc tọa độ (0,0) của scene.
    /// </summary>
    private void CenterMap()
    {
        // 1. Tính tổng kích thước của toàn bộ board
        float totalWidth = gridWidth * cellSize;
        float totalHeight = gridHeight * cellSize; // (Cập nhật) Tính cả chiều cao

        // 2. Tính toán vị trí mới cho BoardManager
        // Dịch lùi lại một nửa kích thước
        // (Trừ thêm 0.5 * cellSize để căn đúng vào tâm prefab)
        Vector3 newPosition = new Vector3(
            (-totalWidth / 2f),
            (-totalHeight / 2f) + padding,
            transform.position.z
        );

        // 3. Áp dụng vị trí mới
        transform.position = newPosition;
    }
    
    private bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }

    /// <summary>
    /// (HÀM MỚI) Kiểm tra xem có còn Spot nào trống trên bàn cờ không
    /// Được gọi bởi GameManager để kiểm tra Game Over.
    /// </summary>
    public bool HasAvailableSpots()
    {
        // Duyệt qua tất cả các ô trong grid
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Nếu tìm thấy 1 spot TỒN TẠI (không null) và CHƯA BỊ CHIẾM
                if (jellySpotGrid[x, y] != null && !jellySpotGrid[x, y].isOccupied)
                {
                    return true; // -> Vẫn còn chỗ, chưa thua
                }
            }
        }
        
        // Nếu duyệt hết mà không còn chỗ
        return false; // -> Hết chỗ, thua
    }
    #endregion
}
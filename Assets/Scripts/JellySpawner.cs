using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Cần cho .GetValues và .ToList

public class JellySpawner : MonoBehaviour
{
    #region Configuration
    [SerializeField] private GameObject jellyPiecePrefab;

    [Header("Tỉ Lệ Spawn Các Loại Khối")]
    [Tooltip("Trọng số (weight) cho 8 loại khối. Càng cao càng dễ ra.\n" +
             "Thứ tự: 0(2x2), 1(Bốn 1x1), 2(Ngang), 3(Dọc), 4(T-Trên), 5(T-Dưới), 6(T-Trái), 7(T-Phải)")]
    [SerializeField] private List<int> pieceSpawnWeights = new List<int>
    {
        10, // Case 0
        10, // Case 1
        10, // Case 2
        10, // Case 3
        10, // Case 4
        10, // Case 5
        10, // Case 6
        10  // Case 7
    };

    [Header("Giới Hạn Màu Sắc")]
    [Tooltip("Danh sách các màu được phép spawn. Nếu danh sách này rỗng, spawner sẽ dùng tất cả các màu trong enum (trừ 'None').")]
    [SerializeField] private List<JellyColor> allowedColors = new List<JellyColor>();

    // Biến nội bộ
    private int totalSpawnWeight = 0;
    private List<JellyColor> validColorPalette = new List<JellyColor>();
    private System.Random rng = new System.Random(); // Dùng cho Fisher-Yates shuffle
    #endregion

    //-------------------------------------------------
    #region Initialization
    //-------------------------------------------------
    
    void Start()
    {
        ValidateAndCalculateTotalWeight();
        BuildValidColorPalette();
        SpawnNewPiece();
    }

    /// <summary>
    /// Kiểm tra danh sách trọng số và tính tổng
    /// </summary>
    private void ValidateAndCalculateTotalWeight()
    {
        if (pieceSpawnWeights == null || pieceSpawnWeights.Count != 8)
        {
            Debug.LogError("Danh sách 'Piece Spawn Weights' phải có đúng 8 phần tử! Đang reset về giá trị mặc định.", this);
            pieceSpawnWeights = Enumerable.Repeat(10, 8).ToList();
        }

        totalSpawnWeight = 0;
        foreach (int weight in pieceSpawnWeights)
        {
            if (weight > 0)
            {
                totalSpawnWeight += weight;
            }
        }

        if (totalSpawnWeight <= 0)
        {
            Debug.LogError("Tổng trọng số (weights) là 0! Vui lòng đặt ít nhất 1 trọng số > 0.", this);
        }
    }

    /// <summary>
    /// Chuẩn bị danh sách màu hợp lệ (chỉ chạy 1 lần)
    /// </summary>
    private void BuildValidColorPalette()
    {
        if (allowedColors != null && allowedColors.Count > 0)
        {
            // Sử dụng danh sách đã cung cấp
            validColorPalette = new List<JellyColor>(allowedColors);
            validColorPalette.Remove(JellyColor.None); // Lọc 'None' nếu user vô tình thêm vào
        }
        else
        {
            // Fallback: Dùng tất cả màu từ enum (trừ 'None')
            var allColors = (JellyColor[])System.Enum.GetValues(typeof(JellyColor));
            validColorPalette = allColors.Skip(1).ToList(); // Skip(1) để bỏ 'None'
        }

        if (validColorPalette.Count == 0)
        {
            Debug.LogError("Không có màu nào (ngoại trừ 'None')! Không thể spawn piece.", this);
        }
        else if (validColorPalette.Count < 4)
        {
            Debug.LogWarning("Có ít hơn 4 màu được phép. Case 1 (Bốn 1x1) có thể có màu trùng lặp.", this);
        }
    }
    #endregion

    //-------------------------------------------------
    #region Spawning Logic
    //-------------------------------------------------

    /// <summary>
    /// Hàm chính: Tạo và gán piece mới
    /// </summary>
    public void SpawnNewPiece()
    {
        if (jellyPiecePrefab == null)
        {
            Debug.LogError("Chưa gán JellyPiece Prefab cho Spawner!", this);
            return;
        }

        // 1. Tạo grid màu
        JellyColor[,] colorGrid = GenerateRandomGrid();
        if (colorGrid == null || colorGrid.Length == 0)
        {
            Debug.LogError("Không thể tạo grid màu. Vui lòng kiểm tra JellyColor enum và 'Allowed Colors'.", this);
            return;
        }

        // 2. Tạo GameObject
        GameObject newPieceObj = Instantiate(jellyPiecePrefab, transform.position, Quaternion.identity);

        // 3. Lấy CẢ HAI component
        JellyPiece newPiece = newPieceObj.GetComponent<JellyPiece>();
        PieceDragger dragger = newPieceObj.GetComponent<PieceDragger>();

        // 4. Kiểm tra và gán
        if (newPiece != null && dragger != null)
        {
            // Gán Spawner cho Dragger (để nó biết vị trí 'startPosition' và báo cáo lại)
            dragger.SetSpawner(this);
            
            // Khởi tạo data màu cho Piece
            newPiece.Initialize(colorGrid);
        }
        else
        {
            Debug.LogError("Prefab 'JellyPiece' thiếu script JellyPiece.cs hoặc PieceDragger.cs!", this);
            Destroy(newPieceObj);
        }
    }

    /// <summary>
    /// Tạo ra một grid 2x2 ngẫu nhiên
    /// </summary>
    private JellyColor[,] GenerateRandomGrid()
    {
        if (validColorPalette.Count == 0)
        {
            return new JellyColor[0, 0]; // Trả về grid rỗng nếu không có màu
        }

        // 1. Xáo trộn danh sách màu (Fisher-Yates Shuffle)
        // Tạo 1 bản copy để xáo trộn, không làm ảnh hưởng list gốc
        List<JellyColor> shuffledColors = new List<JellyColor>(validColorPalette);
        int n = shuffledColors.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1); // Dùng System.Random thay vì Random.Range (ổn định hơn)
            JellyColor value = shuffledColors[k];
            shuffledColors[k] = shuffledColors[n];
            shuffledColors[n] = value;
        }

        // 2. Lấy 4 màu đầu tiên (dùng modulo để an toàn nếu có ít hơn 4 màu)
        JellyColor color1 = shuffledColors[0];
        JellyColor color2 = shuffledColors[1 % shuffledColors.Count];
        JellyColor color3 = shuffledColors[2 % shuffledColors.Count];
        JellyColor color4 = shuffledColors[3 % shuffledColors.Count];

        // 3. Lấy loại piece
        JellyColor[,] grid = new JellyColor[2, 2];
        int pieceType = GetRandomPieceType();

        // 4. Gán màu theo loại piece
        switch (pieceType)
        {
            case 0: // 2x2
                grid[0, 0] = color1; grid[1, 0] = color1;
                grid[0, 1] = color1; grid[1, 1] = color1;
                break;
            case 1: // Bốn 1x1
                grid[0, 0] = color1; grid[1, 0] = color3;
                grid[0, 1] = color2; grid[1, 1] = color4;
                break;
            case 2: // Hai 2x1 (ngang)
                grid[0, 0] = color1; grid[1, 0] = color1;
                grid[0, 1] = color2; grid[1, 1] = color2;
                break;
            case 3: // Hai 1x2 (dọc)
                grid[0, 0] = color1; grid[1, 0] = color2;
                grid[0, 1] = color1; grid[1, 1] = color2;
                break;
            case 4: // T-Trên
                grid[0, 0] = color2; grid[1, 0] = color3;
                grid[0, 1] = color1; grid[1, 1] = color1;
                break;
            case 5: // T-Dưới
                grid[0, 0] = color1; grid[1, 0] = color1;
                grid[0, 1] = color2; grid[1, 1] = color3;
                break;
            case 6: // T-Trái
                grid[0, 0] = color1; grid[1, 0] = color2;
                grid[0, 1] = color1; grid[1, 1] = color3;
                break;
            case 7: // T-Phải
                grid[0, 0] = color2; grid[1, 0] = color1;
                grid[0, 1] = color3; grid[1, 1] = color1;
                break;
        }

        return grid;
    }

    /// <summary>
    /// Chọn một loại piece ngẫu nhiên dựa trên trọng số (weights)
    /// </summary>
    private int GetRandomPieceType()
    {
        if (totalSpawnWeight <= 0)
        {
            Debug.LogWarning("Tổng trọng số là 0, dùng random mặc định (1/8).", this);
            return Random.Range(0, 8); // Dùng Random của Unity
        }

        int randomValue = Random.Range(0, totalSpawnWeight); // Dùng Random của Unity

        for (int i = 0; i < pieceSpawnWeights.Count; i++)
        {
            int currentWeight = pieceSpawnWeights[i];
            if (currentWeight <= 0) continue;

            if (randomValue < currentWeight)
            {
                return i;
            }

            randomValue -= currentWeight;
        }

        return Random.Range(0, 8); // Fallback
    }
    #endregion
}
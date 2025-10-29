using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

[RequireComponent(typeof(JellyPiece))]
public class PieceDragger : MonoBehaviour
{
    #region References
    private JellyPiece jellyPiece;
    private BoardManager boardManager;
    private JellySpawner mySpawner;
    private Camera mainCamera;
    #endregion

    #region State
    private bool isDragging = false;
    private bool isPlaced = false;
    private Vector3 offset;
    private Vector3 startPosition;
    #endregion

    #region Input & Layers
    private Mouse mouse;
    private int jellyLayerMask;
    private int spotLayerMask; // Rất quan trọng cho logic hover
    #endregion

    #region Hover Logic
    // Biến này theo dõi Spot mà chúng ta đang trỏ chuột vào
    private JellySpot currentHoveredSpot = null;
    #endregion

    //-------------------------------------------------
    #region Initialization
    //-------------------------------------------------

    void Awake()
    {
        jellyPiece = GetComponent<JellyPiece>();
        mainCamera = Camera.main;
        mouse = Mouse.current;
        
        jellyLayerMask = LayerMask.GetMask("Jelly"); 
        spotLayerMask = LayerMask.GetMask("JellySpot"); // Cần layer của Spot
    }

    void Start()
    {
        boardManager = FindObjectOfType<BoardManager>();
        if (boardManager == null)
        {
            Debug.LogError("Không tìm thấy BoardManager trong Scene!", this);
        }
    }

    /// <summary>
    /// Được gọi bởi JellySpawner để lưu vị trí ban đầu
    /// </summary>
    public void SetSpawner(JellySpawner spawner)
    {
        mySpawner = spawner;
        startPosition = spawner.transform.position;
    }
    #endregion

    //-------------------------------------------------
    #region Drag & Drop Logic (Core)
    //-------------------------------------------------
    
    void Update()
    {
        // Các kiểm tra an toàn
        if (isPlaced || boardManager == null) return;
        if (mouse == null) { mouse = Mouse.current; if (mouse == null) return; }

        Ray mouseRay = mainCamera.ScreenPointToRay(GetMousePos());

        // 1. Nhấn chuột xuống
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, jellyLayerMask))
            {
                // Phải nhấn trúng piece này
                if (hit.collider.GetComponentInParent<PieceDragger>() == this)
                {
                    isDragging = true;
                    Vector3 mouseWorldPos = GetMouseWorldPositionFromRay(mouseRay);
                    offset = transform.position - mouseWorldPos;
                    
                    // Ẩn TẤT CẢ các border khác khi bắt đầu kéo
                    boardManager.HideAllBorders();
                }
            }
        }

        // 2. Đang kéo chuột
        if (isDragging && mouse.leftButton.isPressed)
        {
            Vector3 mouseWorldPos = GetMouseWorldPositionFromRay(mouseRay);
            transform.position = mouseWorldPos + offset;
            
            // Xử lý logic HOVER (hiện viền 1x1)
            HandleSpotHighlighting_Hover(mouseRay);
        }

        // 3. Thả chuột
        if (isDragging && mouse.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            
            // Xử lý logic ĐẶT
            TryPlaceOnBoard();
        }
    }

    /// <summary>
    /// Xử lý việc bật/tắt viền khi rê chuột
    /// </summary>
    private void HandleSpotHighlighting_Hover(Ray mouseRay)
    {
        JellySpot spotUnderMouse = null;
        
        // Raycast để tìm JellySpot
        if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, spotLayerMask))
        {
            spotUnderMouse = hit.collider.GetComponent<JellySpot>();
        }

        // Nếu spot mới khác spot cũ (chuột di chuyển)
        if (spotUnderMouse != currentHoveredSpot)
        {
            // 1. TẮT spot cũ (nếu có)
            if (currentHoveredSpot != null)
            {
                currentHoveredSpot.HideBorder();
            }

            // 2. BẬT spot mới (nếu hợp lệ)
            if (spotUnderMouse != null && !spotUnderMouse.isOccupied)
            {
                spotUnderMouse.ShowBorder();
                currentHoveredSpot = spotUnderMouse; // Lưu lại spot mới
            }
            else
            {
                // Chuột đang ở trên 1 spot không hợp lệ, hoặc ngoài board
                currentHoveredSpot = null;
            }
        }
    }

    /// <summary>
    /// Thử đặt piece lên bàn cờ
    /// </summary>
    private void TryPlaceOnBoard()
    {
        // Kiểm tra xem lúc thả chuột, ta có đang ở trên 1 spot hợp lệ không
        if (currentHoveredSpot != null && !currentHoveredSpot.isOccupied)
        {
            // Tắt viền của spot đó
            JellySpot targetSpot = currentHoveredSpot;
            targetSpot.HideBorder();
            
            // Gọi hàm đặt của BoardManager
            if (boardManager.TryPlacePiece(jellyPiece, targetSpot))
            {
                isPlaced = true;
                this.enabled = false; // Vô hiệu hóa script này để không kéo được nữa
                
                if (mySpawner != null)
                {
                    mySpawner.SpawnNewPiece();
                }
            }
            else
            {
                // BoardManager từ chối (ví dụ: đang check match)
                ReturnToStart();
            }
        }
        else
        {
             // Thả ở nơi không hợp lệ
             ReturnToStart();
        }
        
        // Dọn dẹp
        currentHoveredSpot = null;
    }

    /// <summary>
    /// Trả piece về vị trí ban đầu
    /// </summary>
    private void ReturnToStart()
    {
        transform.DOMove(startPosition, 0.3f).SetEase(Ease.OutCubic);
    }
    #endregion
    
    //-------------------------------------------------
    #region Input Helpers
    //-------------------------------------------------

    private Vector3 GetMousePos()
    {
        return mouse.position.ReadValue();
    }

    private Vector3 GetMouseWorldPositionFromRay(Ray ray)
    {
        Plane boardPlane = new Plane(Vector3.forward, Vector3.zero);
        if (boardPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        // Fallback
        Vector3 mousePos = GetMousePos();
        Vector3 screenMousePos = new Vector3(mousePos.x, mousePos.y, mainCamera.WorldToScreenPoint(transform.position).z);
        return mainCamera.ScreenToWorldPoint(screenMousePos);
    }

    public void SetIsPlaced(bool placed)
    {
        isPlaced = placed;
    }
    public bool GetIsPlaced()
    {
        return isPlaced;
    }
    #endregion
}
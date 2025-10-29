using UnityEngine;
using UnityEngine.InputSystem; // Giữ nguyên
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
    // (*** SỬA ĐỔI ***)
    private Pointer pointer; // Thay "Mouse mouse" bằng "Pointer pointer"
    // (*** HẾT SỬA ĐỔI ***)

    private int jellyLayerMask;
    private int spotLayerMask;
    #endregion

    #region Hover Logic
    private JellySpot currentHoveredSpot = null;
    #endregion

    //-------------------------------------------------
    #region Initialization
    //-------------------------------------------------

    void Awake()
    {
        jellyPiece = GetComponent<JellyPiece>();
        mainCamera = Camera.main;

        // (*** SỬA ĐỔI ***)
        pointer = Pointer.current; // Thay "Mouse.current" bằng "Pointer.current"
        // (*** HẾT SỬA ĐỔI ***)

        jellyLayerMask = LayerMask.GetMask("Jelly"); 
        spotLayerMask = LayerMask.GetMask("JellySpot");
    }

    void Start()
    {
        boardManager = FindObjectOfType<BoardManager>();
        if (boardManager == null)
        {
            Debug.LogError("Không tìm thấy BoardManager trong Scene!", this);
        }
    }

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
        
        // (*** SỬA ĐỔI: Kiểm tra Pointer ***)
        if (pointer == null) 
        { 
            pointer = Pointer.current; 
            if (pointer == null) return; 
        }

        Ray mouseRay = mainCamera.ScreenPointToRay(GetPointerPos()); // Đổi tên hàm

        // 1. Nhấn chuột / chạm màn hình
        // (*** SỬA ĐỔI: Dùng .press thay vì .leftButton ***)
        if (pointer.press.wasPressedThisFrame)
        // (*** HẾT SỬA ĐỔI ***)
        {
            if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, jellyLayerMask))
            {
                if (hit.collider.GetComponentInParent<PieceDragger>() == this)
                {
                    isDragging = true;
                    Vector3 mouseWorldPos = GetMouseWorldPositionFromRay(mouseRay);
                    offset = transform.position - mouseWorldPos;
                    
                    boardManager.HideAllBorders();
                }
            }
        }

        // 2. Đang kéo / đang lướt tay
        // (*** SỬA ĐỔI: Dùng .press thay vì .leftButton ***)
        if (isDragging && pointer.press.isPressed)
        // (*** HẾT SỬA ĐỔI ***)
        {
            Vector3 mouseWorldPos = GetMouseWorldPositionFromRay(mouseRay);
            transform.position = mouseWorldPos + offset;
            
            HandleSpotHighlighting_Hover(mouseRay);
        }

        // 3. Thả chuột / nhấc tay
        // (*** SỬA ĐỔI: Dùng .press thay vì .leftButton ***)
        if (isDragging && pointer.press.wasReleasedThisFrame)
        // (*** HẾT SỬA ĐỔI ***)
        {
            isDragging = false;
            
            TryPlaceOnBoard();
        }
    }

    private void HandleSpotHighlighting_Hover(Ray mouseRay)
    {
        JellySpot spotUnderMouse = null;
        
        if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, spotLayerMask))
        {
            spotUnderMouse = hit.collider.GetComponent<JellySpot>();
        }

        if (spotUnderMouse != currentHoveredSpot)
        {
            if (currentHoveredSpot != null)
            {
                currentHoveredSpot.HideBorder();
            }
            
            if (spotUnderMouse != null && !spotUnderMouse.isOccupied)
            {
                spotUnderMouse.ShowBorder();
                currentHoveredSpot = spotUnderMouse;
            }
            else
            {
                currentHoveredSpot = null;
            }
        }
    }

    private void TryPlaceOnBoard()
    {
        if (currentHoveredSpot != null && !currentHoveredSpot.isOccupied)
        {
            JellySpot targetSpot = currentHoveredSpot;
            targetSpot.HideBorder();
            
            if (boardManager.TryPlacePiece(jellyPiece, targetSpot))
            {
                isPlaced = true;
                this.enabled = false;
                
                if (mySpawner != null)
                {
                    mySpawner.SpawnNewPiece();
                }
            }
            else
            {
                ReturnToStart();
            }
        }
        else
        {
             ReturnToStart();
        }
        
        currentHoveredSpot = null;
    }
    
    private void ReturnToStart()
    {
        transform.DOMove(startPosition, 0.3f).SetEase(Ease.OutCubic);
    }
    #endregion
    
    //-------------------------------------------------
    #region Input Helpers
    //-------------------------------------------------

    // (*** SỬA ĐỔI: Đổi tên và logic hàm ***)
    private Vector3 GetPointerPos()
    {
        return pointer.position.ReadValue(); // Thay "mouse.position"
    }
    // (*** HẾT SỬA ĐỔI ***)

    private Vector3 GetMouseWorldPositionFromRay(Ray ray)
    {
        Plane boardPlane = new Plane(Vector3.forward, Vector3.zero);
        if (boardPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        // Fallback
        Vector3 pointerPos = GetPointerPos(); // Sửa tên biến
        Vector3 screenPointerPos = new Vector3(pointerPos.x, pointerPos.y, mainCamera.WorldToScreenPoint(transform.position).z);
        return mainCamera.ScreenToWorldPoint(screenPointerPos);
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
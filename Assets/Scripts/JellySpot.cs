using UnityEngine;
using DG.Tweening; // Cần thư viện DOTween

public class JellySpot : MonoBehaviour
{
    #region Configuration
    [Header("Components")]
    [Tooltip("GameObject chứa Renderer của viền (border)")]
    [SerializeField] private GameObject borderObject; 

    [Header("Settings")]
    [Tooltip("Thời gian fade-in / fade-out của viền")]
    [SerializeField] private float fadeDuration = 0.2f;
    
    [Tooltip("Tên của Layer được gán cho Spot (dùng cho Raycast)")]
    [SerializeField] private string spotLayerName = "JellySpot"; 
    #endregion

    #region State
    [Tooltip("Spot này đã bị chiếm bởi 1 JellyPiece chưa?")]
    public bool isOccupied = false;
    
    [Tooltip("Vị trí (x,y) của Spot này trên bàn cờ")]
    public Vector2Int gridPosition;
    #endregion

    #region Private References
    private Material borderMaterial; // Material của viền
    private Tween currentFadeTween;  // Animation đang chạy
    #endregion

    //-------------------------------------------------
    #region Initialization
    //-------------------------------------------------

    void Awake()
    {
        if (borderObject == null)
        {
            Debug.LogError("Chưa gán Border Object cho JellySpot!", this);
            return;
        }

        // 1. Lấy Renderer từ GameObject
        Renderer borderRenderer = borderObject.GetComponent<Renderer>();
        
        if (borderRenderer == null)
        {
            Debug.LogError("Border Object không có component Renderer!", this);
            return;
        }

        // 2. Bật object và lấy Material
        borderObject.SetActive(true); 
        borderMaterial = borderRenderer.material; 
        
        // 3. Đặt alpha về 0 ban đầu
        Color startColor = borderMaterial.color;
        startColor.a = 0f;
        borderMaterial.color = startColor;
        
        // 4. Cài đặt Layer cho chính Spot này (để PieceDragger raycast)
        int layer = LayerMask.NameToLayer(spotLayerName);
        if (layer == -1)
        {
            Debug.LogError($"Layer '{spotLayerName}' chưa được tạo! Hãy vào Edit > Project Settings > Tags and Layers.", this);
        }
        else
        {
            gameObject.layer = layer;
        }
    }
    #endregion

    //-------------------------------------------------
    #region Public API
    //-------------------------------------------------

    /// <summary>
    /// Hiển thị viền (với hiệu ứng fade-in)
    /// </summary>
    public void ShowBorder()
    {
        if (borderMaterial == null) return;

        currentFadeTween?.Kill(); // Hủy animation cũ (nếu có)
        
        // Fade alpha của material lên 1
        currentFadeTween = borderMaterial.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Ẩn viền (với hiệu ứng fade-out)
    /// </summary>
    public void HideBorder()
    {
        if (borderMaterial == null) return;

        currentFadeTween?.Kill(); // Hủy animation cũ (nếu có)
        
        // Fade alpha của material về 0
        currentFadeTween = borderMaterial.DOFade(0f, fadeDuration).SetEase(Ease.InQuad);
    }
    
    #endregion
}
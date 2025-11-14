using UnityEngine;
using DG.Tweening;

public class JellySpot : MonoBehaviour
{
    #region Configuration
    [Header("Components")]
    [SerializeField] private GameObject borderObject; 

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    
    [SerializeField] private string spotLayerName = "JellySpot"; 
    #endregion

    #region State
    public bool isOccupied = false;
    
    public Vector2Int gridPosition;
    #endregion

    #region Private References
    private Material borderMaterial;
    private Tween currentFadeTween;
    #endregion

    //-------------------------------------------------
    #region Initialization
    //-------------------------------------------------

    void Awake()
    {
        if (borderObject == null)
        {
            Debug.LogError("Border Object not assigned to JellySpot!", this);
            return;
        }

        Renderer borderRenderer = borderObject.GetComponent<Renderer>();
        
        if (borderRenderer == null)
        {
            Debug.LogError("Border Object is missing a Renderer component!", this);
            return;
        }

        borderObject.SetActive(true); 
        borderMaterial = borderRenderer.material; 
        
        Color startColor = borderMaterial.color;
        startColor.a = 0f;
        borderMaterial.color = startColor;
        
        int layer = LayerMask.NameToLayer(spotLayerName);
        if (layer == -1)
        {
            Debug.LogError($"Layer '{spotLayerName}' not found! Please create it in Edit > Project Settings > Tags and Layers.", this);
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

    public void ShowBorder()
    {
        if (borderMaterial == null) return;

        currentFadeTween?.Kill();
        
        currentFadeTween = borderMaterial.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
    }

    public void HideBorder()
    {
        if (borderMaterial == null) return;

        currentFadeTween?.Kill();
        
        currentFadeTween = borderMaterial.DOFade(0f, fadeDuration).SetEase(Ease.InQuad);
    }
    
    #endregion
}
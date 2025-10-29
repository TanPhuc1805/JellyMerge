using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
// using UnityEngine.SceneManagement; // Không cần thiết ở đây nữa, SceneLoader đã xử lý

[RequireComponent(typeof(SceneLoader))] 
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // --- References (Goal Panel) ---
    [Header("Goal Panel References")]
    [SerializeField] private GameObject goalIconPrefab;
    [SerializeField] private Transform goalPanelContainer;
    private Dictionary<JellyColor, GoalIcon> _spawnedIcons = new Dictionary<JellyColor, GoalIcon>();

    // --- References (UI) ---
    [Header("Scene Loading")]
    private SceneLoader sceneLoader; 

    [Header("UI Panels")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject gameOverPanel;
    
    // (*** MỚI: Thêm nền mờ ***)
    [Tooltip("Gán Image nền đen mờ (fullscreen) vào đây")]
    [SerializeField] private Image backgroundDimmer;

    [Header("UI Buttons")]
    [SerializeField] private Button infoReplayButton;
    [SerializeField] private Button infoNextLevelButton;
    [SerializeField] private Button winNextLevelButton;
    [SerializeField] private Button gameOverReplayButton;

    [Header("UI Texts (TMP)")]
    [SerializeField] private TextMeshProUGUI infoLevelText;
    [SerializeField] private TextMeshProUGUI winLevelText;
    
    [Header("DOTween Settings")]
    [SerializeField] private float panelFadeDuration = 0.4f;
    [SerializeField] private float panelScaleDuration = 0.4f;
    [SerializeField] private Ease panelEase = Ease.OutBack;

    #region Initialization
    void Awake()
    {
        if (Instance != null && Instance != this) 
            Destroy(gameObject);
        else 
            Instance = this;
        
        sceneLoader = GetComponent<SceneLoader>();
    }

    void Start()
    {
        // Gắn hàm cho các Button
        if(infoReplayButton != null) infoReplayButton.onClick.AddListener(sceneLoader.ReloadScene);
        if(infoNextLevelButton != null) infoNextLevelButton.onClick.AddListener(sceneLoader.LoadNextLevel);
        if(winNextLevelButton != null) winNextLevelButton.onClick.AddListener(sceneLoader.LoadNextLevel);
        if(gameOverReplayButton != null) gameOverReplayButton.onClick.AddListener(sceneLoader.ReloadScene);

        // Dọn dẹp các panel
        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(true);
        
        // (*** MỚI: Ẩn nền mờ ban đầu ***)
        if (backgroundDimmer != null) backgroundDimmer.gameObject.SetActive(false);
    }

    /// <summary>
    /// Được gọi bởi GameManager lúc Start
    /// </summary>
    public void SetupInitialUI(int levelIndex) 
    {
        // 1. Ẩn các panel bật lên
        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(true); 

        // 2. Cài đặt Level Text
        string levelString = $"LEVEL {levelIndex}";

        if (infoLevelText != null) infoLevelText.text = levelString;
        if (winLevelText != null) winLevelText.text = levelString;
    }
    #endregion

    #region Goal Panel (Giữ Nguyên)
    
    public void InitializeGoalPanel(Dictionary<JellyColor, int> requirements)
    {
        if (goalIconPrefab == null || goalPanelContainer == null)
        {
            Debug.LogError("Chưa gán Prefab hoặc Container cho UIManager!");
            return;
        }
        
        foreach (Transform child in goalPanelContainer)
        {
            Destroy(child.gameObject);
        }
        _spawnedIcons.Clear();
        
        foreach (var req in requirements)
        {
            JellyColor color = req.Key;
            int amount = req.Value;

            GameObject iconObj = Instantiate(goalIconPrefab, goalPanelContainer);
            GoalIcon goalIcon = iconObj.GetComponent<GoalIcon>();
            
            if (goalIcon != null)
            {
                goalIcon.SetGoal(color, amount);
                _spawnedIcons[color] = goalIcon;
            }
        }
    }
    
    public void UpdateGoalUI(Dictionary<JellyColor, int> updatedRequirements)
    {
        foreach (var req in updatedRequirements)
        {
            JellyColor color = req.Key;
            int newAmount = req.Value;
            
            if (_spawnedIcons.TryGetValue(color, out GoalIcon icon))
            {
                if (newAmount >= 0)
                {
                    icon.UpdateAmount(newAmount);
                }
            }
        }
    }
    #endregion

    #region Panel Control (Đã Cập Nhật)

    /// <summary>
    /// Được gọi bởi GameManager khi thắng
    /// </summary>
    public void ShowWinPanel()
    {
        // (*** MỚI: Tắt InfoPanel và Bật nền mờ ***)
        if (infoPanel != null) infoPanel.SetActive(false);
        if (backgroundDimmer != null) backgroundDimmer.gameObject.SetActive(true);
        
        if (winPanel != null)
        {
            // (*** SỬA ĐỔI: Thêm targetScale 1.0f ***)
            AnimatePanelIn(winPanel, 1.0f);
        }
    }

    /// <summary>
    /// Được gọi bởi GameManager khi thua
    /// </summary>
    public void ShowGameOverPanel()
    {
        // (*** MỚI: Tắt InfoPanel và Bật nền mờ ***)
        if (infoPanel != null) infoPanel.SetActive(false);
        if (backgroundDimmer != null) backgroundDimmer.gameObject.SetActive(true);
        
        if (gameOverPanel != null)
        {
            // (*** SỬA ĐỔI: Thêm targetScale 2.0f ***)
            AnimatePanelIn(gameOverPanel, 2.0f);
        }
    }

    /// <summary>
    /// (SỬA ĐỔI) Hàm helper dùng DOTween để hiện panel với scale tùy chỉnh
    /// </summary>
    private void AnimatePanelIn(GameObject panel, float targetScale)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = panel.AddComponent<CanvasGroup>();
        }
        
        panel.SetActive(true);
        cg.alpha = 0f;
        panel.transform.localScale = Vector3.one * 0.8f; // Bắt đầu nhỏ
        
        // Fade in
        cg.DOFade(1f, panelFadeDuration).SetEase(Ease.OutQuad);
        
        // (*** SỬA ĐỔI: Dùng targetScale ***)
        panel.transform.DOScale(targetScale, panelScaleDuration)
            .SetEase(panelEase) 
            .SetDelay(0.1f);
    }
    
    #endregion
}
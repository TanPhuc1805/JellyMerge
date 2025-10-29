using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Quản lý trạng thái chung của trò chơi (Playing, Win, GameOver)
/// và theo dõi mục tiêu của màn chơi.
/// </summary>
public class GameManager : MonoBehaviour
{
    // --- STATE ---
    public enum GameState
    {
        Playing,
        Win,
        GameOver
    }

    [Header("Level Configuration")]
    [Tooltip("Số thứ tự của màn chơi này (ví dụ: 1, 2, 3...)")]
    public int levelNumber = 1;

    [Tooltip("Trạng thái hiện tại của game (Chỉ để theo dõi)")]
    [SerializeField] 
    private GameState _currentState; // Dùng _ (gạch dưới) cho private

    public GameState CurrentState 
    { 
        get { return _currentState; }
        private set { _currentState = value; } // Setter vẫn là private
    }


    // --- SINGLETON ---
    public static GameManager Instance { get; private set; }

    // --- REFERENCES ---
    [Header("References")]
    [Tooltip("Gán BoardManager vào đây")]
    [SerializeField] private BoardManager boardManager;
    // (*** MỚI: Tự động tìm UIManager ***)
    private UIManager uiManager;


    // --- LEVEL REQUIREMENTS ---
    [System.Serializable]
    public class ColorRequirement
    {
        public JellyColor color;
        public int amount;
    }

    [Header("Level Requirements")]
    [Tooltip("Danh sách các mục tiêu cần thu thập của màn chơi này")]
    public List<ColorRequirement> levelRequirements;
    
    // Dictionary nội bộ để theo dõi tiến trình
    private Dictionary<JellyColor, int> _currentRequirements;

    #region Initialization
    void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        CurrentState = GameState.Playing; 
    }

    void Start()
    {
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }

        // (*** MỚI: Tự động tìm UIManager ***)
        uiManager = UIManager.Instance; // Ưu tiên Singleton

        InitializeRequirements();

        // (*** MỚI: Khởi tạo UI ***)
        if (uiManager != null)
        {
            uiManager.SetupInitialUI(levelNumber);
            uiManager.InitializeGoalPanel(_currentRequirements);
        }
        else
        {
            Debug.LogWarning("Không tìm thấy UIManager, Goal Panel sẽ không hoạt động.");
        }
    }
    
    void InitializeRequirements()
    {
        _currentRequirements = new Dictionary<JellyColor, int>();
        foreach (var req in levelRequirements)
        {
            if (req.color != JellyColor.None && req.amount > 0)
            {
                _currentRequirements[req.color] = req.amount;
            }
        }
    }
    #endregion

    #region Public API (Called by BoardManager)
    
    public void ReportColorsCleared(Dictionary<JellyColor, int> clearedColors)
    {
        if (CurrentState != GameState.Playing) return;

        bool requirementChanged = false;

        foreach (var pair in clearedColors)
        {
            JellyColor color = pair.Key;
            int amount = pair.Value;
            
            if (_currentRequirements.ContainsKey(color))
            {
                // (SỬA ĐỔI) Chỉ trừ nếu lớn hơn 0
                if (_currentRequirements[color] > 0)
                {
                    _currentRequirements[color] -= amount;
                    if (_currentRequirements[color] < 0)
                    {
                        _currentRequirements[color] = 0; 
                    }
                    requirementChanged = true;
                }
            }
        }

        if (requirementChanged)
        {
            // (*** MỚI: Cập nhật UI ***)
            if (uiManager != null)
            {
                uiManager.UpdateGoalUI(_currentRequirements);
            }

            CheckForWin();
        }
    }
    
    public void CheckForGameOver()
    {
        if (CurrentState != GameState.Playing) return;

        if (boardManager != null && !boardManager.HasAvailableSpots())
        {
            TriggerGameOver();
        }
    }

    #endregion

    #region Win/Lose Logic
    
    private void CheckForWin()
    {
        if (CurrentState != GameState.Playing) return;
        
        bool hasRemainingGoals = _currentRequirements.Values.Any(amount => amount > 0);

        if (!hasRemainingGoals)
        {
            TriggerWin();
        }
    }

    private void TriggerWin()
    {
        CurrentState = GameState.Win;
        Debug.Log("--- GAME WIN! ---");

        if (uiManager != null)
        {
            uiManager.ShowWinPanel();
        }
    }


    private void TriggerGameOver()
    {
        CurrentState = GameState.GameOver;
        Debug.Log("--- GAME OVER! (Board Full) ---");

        if (uiManager != null)
        {
            uiManager.ShowGameOverPanel();
        }
    }
    
    #endregion
}
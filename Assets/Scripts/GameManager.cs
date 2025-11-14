using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Playing,
        Win,
        GameOver
    }

    [Header("Level Configuration")]
    public int levelNumber = 1;

    [SerializeField] 
    private GameState _currentState;

    public GameState CurrentState 
    { 
        get { return _currentState; }
        private set { _currentState = value; }
    }


    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    private UIManager uiManager;


    [System.Serializable]
    public class ColorRequirement
    {
        public JellyColor color;
        public int amount;
    }

    [Header("Level Requirements")]
    public List<ColorRequirement> levelRequirements;
    
    private Dictionary<JellyColor, int> _currentRequirements;

    #region Initialization
    void Awake()
    {
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

        uiManager = UIManager.Instance;

        InitializeRequirements();

        if (uiManager != null)
        {
            uiManager.SetupInitialUI(levelNumber);
            uiManager.InitializeGoalPanel(_currentRequirements);
        }
        else
        {
            Debug.LogWarning("UIManager not found, Goal Panel will not work.");
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
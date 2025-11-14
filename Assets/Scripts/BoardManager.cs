using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;

public class BoardManager : MonoBehaviour
{
    [System.Serializable]
    public class BoardCellData
    {
        public Vector2Int position;
        public bool isEnabled = true;
        public bool hasInitialPiece = false;

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

    public float padding = 3f;

    [Header("Prefabs")]
    public GameObject jellySpotPrefab;
    public GameObject jellyPiecePrefab;
    
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Match Logic")]
    [SerializeField] private float timeBetweenCombos = 0.5f;

    private JellyPiece[,] pieceGrid;
    private JellySpot[,] jellySpotGrid;

    [HideInInspector]
    public List<BoardCellData> levelLayout = new List<BoardCellData>();

    private bool isCheckingMatches = false;
    #endregion

    //-------------------------------------------------
    #region Board Initialization
    //-------------------------------------------------
    
    void Start()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }
        }
        
        CreateBoard();
        CenterMap();
    }

    private void CreateBoard()
    {
        pieceGrid = new JellyPiece[gridWidth, gridHeight];
        jellySpotGrid = new JellySpot[gridWidth, gridHeight];

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
                
                if (!layoutDict.TryGetValue(pos, out cellData))
                {
                    cellData = new BoardCellData { position = pos, isEnabled = true };
                }

                if (cellData.isEnabled)
                {
                    Vector3 position = new Vector3(x * cellSize, y * cellSize, 0);
                    GameObject spotObj = Instantiate(jellySpotPrefab, position, Quaternion.identity, transform);
                    spotObj.name = $"Spot_{x}_{y}";
                    spotObj.transform.position += new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0);

                    JellySpot spot = spotObj.GetComponent<JellySpot>();
                    if (spot != null)
                    {
                        spot.gridPosition = new Vector2Int(x, y);
                        jellySpotGrid[x, y] = spot;
                    }

                    if (cellData.hasInitialPiece)
                    {
                        if (jellyPiecePrefab != null)
                        {
                            GameObject pieceObj = Instantiate(jellyPiecePrefab, spot.transform.position, Quaternion.identity);
                            JellyPiece piece = pieceObj.GetComponent<JellyPiece>();
                            
                            JellyColor[,] colors = new JellyColor[2, 2];
                            colors[0, 1] = cellData.initial_TL;
                            colors[1, 1] = cellData.initial_TR;
                            colors[0, 0] = cellData.initial_BL;
                            colors[1, 0] = cellData.initial_BR;
                            piece.Initialize(colors);

                            PlaceInitialPiece(piece, spot);
                        }
                        else
                        {
                            Debug.LogError("jellyPiecePrefab is not assigned on BoardManager!", this);
                        }
                    }
                }
            }
        }
    }

    private void PlaceInitialPiece(JellyPiece piece, JellySpot spot)
    {
        Vector2Int targetPos = spot.gridPosition;

        pieceGrid[targetPos.x, targetPos.y] = piece;
        spot.isOccupied = true;
        
        piece.baseBoardPosition = targetPos;
        
        piece.transform.SetParent(spot.transform);
        piece.transform.position = spot.transform.position;
        
        PieceDragger dragger = piece.GetComponent<PieceDragger>();
        if(dragger != null)
        {
            dragger.SetIsPlaced(true);
            dragger.enabled = false; 
        }
    }
    #endregion

    //-------------------------------------------------
    #region Piece Placement (1x1 Logic)
    //-------------------------------------------------

    public bool TryPlacePiece(JellyPiece piece, JellySpot spot)
    {
        if (isCheckingMatches || spot == null || spot.isOccupied || 
            (gameManager != null && gameManager.CurrentState != GameManager.GameState.Playing))
        {
            return false;
        }
        
        Vector2Int targetPos = spot.gridPosition;

        pieceGrid[targetPos.x, targetPos.y] = piece;
        spot.isOccupied = true;
        
        piece.baseBoardPosition = targetPos;
        
        piece.transform.SetParent(spot.transform);
        piece.transform.DOMove(spot.transform.position, 0.2f).SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                StartCoroutine(ProcessBoardMatches());
            });
        
        return true;
    }

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
    #region Match & Combo Logic
    //-------------------------------------------------

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

    private IEnumerator ProcessBoardMatches()
    {
        isCheckingMatches = true;

        while (true)
        {
            List<ClearCommand> commands = FindAllEdgeMatches();
            if (commands.Count == 0)
            {
                break;
            }

            var commandsByPiece = commands.Distinct()
                .GroupBy(c => c.piece)
                .ToDictionary(
                    g => g.Key, 
                    g => g.Select(cmd => new Vector2Int(cmd.localX, cmd.localY)).ToList()
                );

            var updateResults = new List<(JellyPiece, PieceUpdateResult)>();
            
            Dictionary<JellyColor, int> totalClearedThisTurn = new Dictionary<JellyColor, int>();

            foreach (var pair in commandsByPiece)
            {
                JellyPiece piece = pair.Key;
                List<Vector2Int> coordsToClear = pair.Value;

                if (piece == null) continue;

                HashSet<Vector2Int> countedCellsOnThisPiece = new HashSet<Vector2Int>();

                foreach (var coord in coordsToClear)
                {
                    if (countedCellsOnThisPiece.Contains(coord))
                    {
                        continue;
                    }

                    JellyColor color = piece.GetColorAt(coord.x, coord.y);
                    if (color != JellyColor.None)
                    {
                        totalClearedThisTurn.TryGetValue(color, out int currentCount);
                        totalClearedThisTurn[color] = currentCount + 1;

                        List<Vector2Int> blockCells = piece.GetLinkedCells(coord.x, coord.y);
                        
                        foreach (var cell in blockCells)
                        {
                            countedCellsOnThisPiece.Add(cell);
                        }
                    }
                }
                
                PieceUpdateResult result = piece.ApplyLogicUpdate(coordsToClear);
                
                updateResults.Add((piece, result));
            }


            if (totalClearedThisTurn.Count > 0 && gameManager != null)
            {
                gameManager.ReportColorsCleared(totalClearedThisTurn);
            }

            foreach (var (piece, result) in updateResults)
            {
                if (piece == null) continue;
                
                if (result.wasDestroyed)
                {
                    piece.PlayDestroySequence();
                    
                    Vector2Int pos = piece.baseBoardPosition;
                    if(IsValidPosition(pos))
                    {
                        pieceGrid[pos.x, pos.y] = null;
                        if(jellySpotGrid[pos.x, pos.y] != null)
                        {
                            jellySpotGrid[pos.x, pos.y].isOccupied = false;
                        }
                    }
                }
                else if (result.wasModified)
                {
                    piece.PlayVisualUpdate();
                }
            }

            yield return new WaitForSeconds(1.1f); 
            
            yield return new WaitForSeconds(timeBetweenCombos);
        }

        isCheckingMatches = false;

        if (gameManager != null)
        {
            gameManager.CheckForGameOver();
        }
    }

    private List<ClearCommand> FindAllEdgeMatches()
    {
        List<ClearCommand> commands = new List<ClearCommand>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                JellyPiece pieceA = pieceGrid[x, y];
                if (pieceA == null) continue;

                if (x + 1 < gridWidth)
                {
                    JellyPiece pieceB = pieceGrid[x + 1, y];
                    if (pieceB != null)
                    {
                        CheckAndAdd(commands, pieceA, 1, 0, pieceB, 0, 0);
                        CheckAndAdd(commands, pieceA, 1, 1, pieceB, 0, 1);
                    }
                }
                
                if (y + 1 < gridHeight)
                {
                    JellyPiece pieceC = pieceGrid[x, y + 1];
                    if (pieceC != null)
                    {
                        CheckAndAdd(commands, pieceA, 0, 1, pieceC, 0, 0);
                        CheckAndAdd(commands, pieceA, 1, 1, pieceC, 1, 0);
                    }
                }
            }
        }
        return commands;
    }

    private void CheckAndAdd(List<ClearCommand> list,
                             JellyPiece pieceA, int ax, int ay,
                             JellyPiece pieceB, int bx, int by)
    {
        JellyColor colorA = pieceA.GetColorAt(ax, ay);

        if (colorA != JellyColor.None && colorA == pieceB.GetColorAt(bx, by))
        {
            List<Vector2Int> linkedCellsA = pieceA.GetLinkedCells(ax, ay);
            foreach (var cellPos in linkedCellsA)
            {
                list.Add(new ClearCommand(pieceA, cellPos.x, cellPos.y));
            }

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

    private void CenterMap()
    {
        float totalWidth = gridWidth * cellSize;
        float totalHeight = gridHeight * cellSize;

        Vector3 newPosition = new Vector3(
            (-totalWidth / 2f),
            (-totalHeight / 2f) + padding,
            transform.position.z
        );

        transform.position = newPosition;
    }
    
    private bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }

    public bool HasAvailableSpots()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (jellySpotGrid[x, y] != null && !jellySpotGrid[x, y].isOccupied)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    #endregion
}
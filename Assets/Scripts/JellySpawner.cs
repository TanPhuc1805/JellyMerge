using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class JellySpawner : MonoBehaviour
{
    #region Configuration
    [SerializeField] private GameObject jellyPiecePrefab;

    [Header("Spawn Weights")]
    [SerializeField] private List<int> pieceSpawnWeights = new List<int>
    {
        10,
        10,
        10,
        10,
        10,
        10,
        10,
        10
    };

    [Header("Color Limits")]
    [SerializeField] private List<JellyColor> allowedColors = new List<JellyColor>();

    private int totalSpawnWeight = 0;
    private List<JellyColor> validColorPalette = new List<JellyColor>();
    private System.Random rng = new System.Random();
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

    private void ValidateAndCalculateTotalWeight()
    {
        if (pieceSpawnWeights == null || pieceSpawnWeights.Count != 8)
        {
            Debug.LogError("'Piece Spawn Weights' must have exactly 8 elements! Resetting to default.", this);
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
            Debug.LogError("Total spawn weight is 0! Please set at least one weight > 0.", this);
        }
    }

    private void BuildValidColorPalette()
    {
        if (allowedColors != null && allowedColors.Count > 0)
        {
            validColorPalette = new List<JellyColor>(allowedColors);
            validColorPalette.Remove(JellyColor.None);
        }
        else
        {
            var allColors = (JellyColor[])System.Enum.GetValues(typeof(JellyColor));
            validColorPalette = allColors.Skip(1).ToList();
        }

        if (validColorPalette.Count == 0)
        {
            Debug.LogError("No valid colors (excluding 'None')! Cannot spawn piece.", this);
        }
        else if (validColorPalette.Count < 4)
        {
            Debug.LogWarning("Fewer than 4 allowed colors. Case 1 (Four 1x1) may have duplicate colors.", this);
        }
    }
    #endregion

    //-------------------------------------------------
    #region Spawning Logic
    //-------------------------------------------------

    public void SpawnNewPiece()
    {
        if (jellyPiecePrefab == null)
        {
            Debug.LogError("JellyPiece Prefab not assigned to Spawner!", this);
            return;
        }

        JellyColor[,] colorGrid = GenerateRandomGrid();
        if (colorGrid == null || colorGrid.Length == 0)
        {
            Debug.LogError("Failed to generate color grid. Check JellyColor enum and 'Allowed Colors'.", this);
            return;
        }

        GameObject newPieceObj = Instantiate(jellyPiecePrefab, transform.position, Quaternion.identity);

        JellyPiece newPiece = newPieceObj.GetComponent<JellyPiece>();
        PieceDragger dragger = newPieceObj.GetComponent<PieceDragger>();

        if (newPiece != null && dragger != null)
        {
            dragger.SetSpawner(this);
            
            newPiece.Initialize(colorGrid);
        }
        else
        {
            Debug.LogError("JellyPiece prefab is missing JellyPiece.cs or PieceDragger.cs script!", this);
            Destroy(newPieceObj);
        }
    }

    private JellyColor[,] GenerateRandomGrid()
    {
        if (validColorPalette.Count == 0)
        {
            return new JellyColor[0, 0];
        }

        List<JellyColor> shuffledColors = new List<JellyColor>(validColorPalette);
        int n = shuffledColors.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            JellyColor value = shuffledColors[k];
            shuffledColors[k] = shuffledColors[n];
            shuffledColors[n] = value;
        }

        JellyColor color1 = shuffledColors[0];
        JellyColor color2 = shuffledColors[1 % shuffledColors.Count];
        JellyColor color3 = shuffledColors[2 % shuffledColors.Count];
        JellyColor color4 = shuffledColors[3 % shuffledColors.Count];

        JellyColor[,] grid = new JellyColor[2, 2];
        int pieceType = GetRandomPieceType();

        switch (pieceType)
        {
            case 0:
                grid[0, 0] = color1; grid[1, 0] = color1;
                grid[0, 1] = color1; grid[1, 1] = color1;
                break;
            case 1:
                grid[0, 0] = color1; grid[1, 0] = color3;
                grid[0, 1] = color2; grid[1, 1] = color4;
                break;
            case 2:
                grid[0, 0] = color1; grid[1, 0] = color1;
                grid[0, 1] = color2; grid[1, 1] = color2;
                break;
            case 3:
                grid[0, 0] = color1; grid[1, 0] = color2;
                grid[0, 1] = color1; grid[1, 1] = color2;
                break;
            case 4:
                grid[0, 0] = color2; grid[1, 0] = color3;
                grid[0, 1] = color1; grid[1, 1] = color1;
                break;
            case 5:
                grid[0, 0] = color1; grid[1, 0] = color1;
                grid[0, 1] = color2; grid[1, 1] = color3;
                break;
            case 6:
                grid[0, 0] = color1; grid[1, 0] = color2;
                grid[0, 1] = color1; grid[1, 1] = color3;
                break;
            case 7:
                grid[0, 0] = color2; grid[1, 0] = color1;
                grid[0, 1] = color3; grid[1, 1] = color1;
                break;
        }

        return grid;
    }

    private int GetRandomPieceType()
    {
        if (totalSpawnWeight <= 0)
        {
            Debug.LogWarning("Total weight is 0, using default random (1/8).", this);
            return Random.Range(0, 8);
        }

        int randomValue = Random.Range(0, totalSpawnWeight);

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

        return Random.Range(0, 8);
    }
    #endregion
}
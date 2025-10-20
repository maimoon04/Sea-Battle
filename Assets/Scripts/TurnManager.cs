using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum TurnState { Player1Placement, Player2Placement, Battle }
public enum GameMode { PvP, PvAI }

public class TurnManager : MonoBehaviour
{
    [Header("Player A (first)")]
    public ShipSpawner player1Spawner;
    public GridController player1Grid;
    public Button player1ReadyButton;

    public CannonController player1Cannon;

    [Header("Player B (second)")]
    public ShipSpawner player2Spawner;
    public GridController player2Grid;
    public Button player2ReadyButton;

    public CannonController player2Cannon;

    public TurnState State { get; private set; } = TurnState.Player1Placement;
    public GameMode gameMode = GameMode.PvP;

    // 1 = player1's turn, 2 = player2's turn
    public int currentBattlePlayer = 1;

    // Score tracking
    public int player1Score = 0;
    public int player2Score = 0;
    public bool gameOver = false;

    void Start()
    {
        SetGameMode(gameMode);
        // Hook up ready buttons
        if (player1ReadyButton != null) player1ReadyButton.onClick.AddListener(() => OnPlayerReady(1));
        if (player2ReadyButton != null) player2ReadyButton.onClick.AddListener(() => OnPlayerReady(2));

        // You can set gameMode from UI before this
        EnterPlayer1Placement();
    }

    // Call this from UI to set game mode before game starts
    public void SetGameMode(GameMode mode)
    {
        gameMode = mode;
    }

    void EnterPlayer1Placement()
    {
        State = TurnState.Player1Placement;
        // spawn for player 1
        if (player1Spawner != null) player1Spawner.SpawnAll();

        // enable player1 grid and spawner UI, disable player2
        SetPlacementEnabled(player1Grid, player1Spawner, true);
        SetPlacementEnabled(player2Grid, player2Spawner, false);

        UpdateReadyButtons();
    }

    void EnterPlayer2Placement()
    {
        State = TurnState.Player2Placement;
         if (player2Spawner != null) player2Spawner.SpawnAll();
            SetPlacementEnabled(player1Grid, player1Spawner, false);
        SetPlacementEnabled(player2Grid, player2Spawner, true);
            
        if (gameMode == GameMode.PvAI)
        {
            // AI: place ships randomly
            if (player2Spawner != null && player2Grid != null)
            {
                PlaceShipsRandomly(player2Spawner, player2Grid);
            }
            // Skip ready button, go straight to battle
            EnterBattle();
        }
        else
        {
            // PvP: normal placement
            UpdateReadyButtons();
        }
    }
    // Randomly place all ships for the AI
    void PlaceShipsRandomly(ShipSpawner spawner, GridController grid)
    {
        System.Random rand = new System.Random();
        foreach (var ship in spawner.spawnedShips)
        {
            bool placed = false;
            int attempts = 0;
            while (!placed && attempts < 50)
            {
                Debug.Log($"Placing ship {ship.shipData.shipName}, attempt {attempts + 1}");
                bool vertical = rand.Next(0, 2) == 0;
                int maxX = vertical ? grid.columns - ship.shipData.length : grid.columns - 1;
                int maxY = vertical ? grid.rows - 1 : grid.rows - ship.shipData.length;
                int x = rand.Next(0, maxX + 1);
                int y = rand.Next(0, maxY + 1);
                Vector2Int start = new Vector2Int(x, y);
                ship.isVertical = vertical;
                placed = grid.TryPlaceShip(ship, start, vertical);
                attempts++;
            }
        }
    }

    void EnterBattle()
    {
        State = TurnState.Battle;
        // both grids active for firing turns; spawners disabled
        SetPlacementEnabled(player1Grid, player1Spawner, false);
        SetPlacementEnabled(player2Grid, player2Spawner, false);

        UpdateReadyButtons();

        // Subscribe to cell click events for both grids
        if (player1Grid != null)
            player1Grid.OnCellClicked.AddListener(OnPlayer1GridCellClicked);
        if (player2Grid != null)
            player2Grid.OnCellClicked.AddListener(OnPlayer2GridCellClicked);

        currentBattlePlayer = 1; // Player 1 starts
        UpdateBattleUI();
    }

    void SetPlacementEnabled(GridController grid, ShipSpawner spawner, bool enabled)
    {
        if (grid != null)
        {
            grid.allowPlacement = enabled;
        }
    }

    // Called when a cell is clicked on player 2's grid (player 1 fires)
    void OnPlayer2GridCellClicked(Vector2Int coord)
    {
        if (State == TurnState.Battle && currentBattlePlayer == 2) return;
        if (gameOver) return;
        if (player1Cannon != null && player2Grid != null)
        {
            player1Cannon.targetGrid = player2Grid;
            player1Cannon.shipSpawner = player2Spawner;
            player1Cannon.FireAtCell(coord);
            NextBattleTurn();
            // If AI mode, let AI fire after player 1
            if (gameMode == GameMode.PvAI && !gameOver)
            {
                StartCoroutine(AIFireCoroutine());
            }
        }
    }

    // Called when a cell is clicked on player 1's grid (player 2 fires)
    void OnPlayer1GridCellClicked(Vector2Int coord)
    {
        if (gameMode == GameMode.PvAI) return; // AI never clicks
        if (State == TurnState.Battle && currentBattlePlayer == 1) return;
        if (gameOver) return;
        if (player2Cannon != null && player1Grid != null)
        {
            player2Cannon.targetGrid = player1Grid;
            player2Cannon.shipSpawner = player1Spawner;
            player2Cannon.FireAtCell(coord);
            NextBattleTurn();
        }
    }
    // AI fires at a random valid cell on player 1's grid
    IEnumerator AIFireCoroutine()
    {
        yield return new WaitForSeconds(1f); // AI delay for realism
        List<Vector2Int> validTargets = new List<Vector2Int>();
        for (int x = 0; x < player1Grid.columns; x++)
        {
            for (int y = 0; y < player1Grid.rows; y++)
            {
                var cell = player1Grid.GetCell(new Vector2Int(x, y));
                if (cell != null && (cell.State == CellState.Empty || cell.State == CellState.Ship))
                {
                    validTargets.Add(new Vector2Int(x, y));
                }
            }
        }
        if (validTargets.Count > 0)
        {
            var rand = new System.Random();
            var target = validTargets[rand.Next(validTargets.Count)];
            player2Cannon.targetGrid = player1Grid;
            player2Cannon.shipSpawner = player1Spawner;
            player2Cannon.FireAtCell(target);
            NextBattleTurn();
        }
    }
    // Called by CannonController when a ship is sunk
    public void OnShipSunk(GridController defenderGrid, Ship sunkShip)
    {
        if (gameOver) return;
        // Determine which player is the attacker and which is the defender
        bool defenderIsPlayer1 = (defenderGrid == player1Grid);
        if (!defenderIsPlayer1)
        {
            // Player 1 sunk a ship
            player1Score++;
            Debug.Log($"Player 1 sunk a ship! Score: {player1Score}");
            if (AllShipsSunk(player2Spawner))
            {
                gameOver = true;
                Debug.Log("Player 1 wins!");
                OnGameOver(1);
            }
        }
        else
        {
            // Player 2 sunk a ship
            player2Score++;
            Debug.Log($"Player 2 sunk a ship! Score: {player2Score}");
            if (AllShipsSunk(player1Spawner))
            {
                gameOver = true;
                Debug.Log("Player 2 wins!");
                OnGameOver(2);
            }
        }
        UpdateBattleUI();
    }

    // Returns true if all ships in the spawner are sunk
    bool AllShipsSunk(ShipSpawner spawner)
    {
        if (spawner == null || spawner.spawnedShips == null) return false;
        foreach (var ship in spawner.spawnedShips)
        {
            if (ship != null && !ship.IsSunk()) return false;
        }
        return true;
    }

    // Called when a player wins
    void OnGameOver(int winner)
    {
        // Add UI or logic for end of game here
        Debug.Log($"Game Over! Player {winner} wins.");
    }

    void NextBattleTurn()
    {
        // Alternate turn
        currentBattlePlayer = (currentBattlePlayer == 1) ? 2 : 1;
        UpdateBattleUI();
    }

    void UpdateBattleUI()
    {
        // Optionally: highlight current player's grid, show turn indicator, etc.
        // For now, you can add UI feedback here if desired.
    }

    void UpdateReadyButtons()
    {
        if (player1ReadyButton != null)
            player1ReadyButton.gameObject.SetActive(State == TurnState.Player1Placement);
        if (player2ReadyButton != null)
            player2ReadyButton.gameObject.SetActive(State == TurnState.Player2Placement);
    }

    void OnPlayerReady(int playerIndex)
    {
        if (playerIndex == 1 && State == TurnState.Player1Placement)
        {
            // ensure all ships placed
            if (player1Spawner != null && !player1Spawner.AllShipsPlaced())
            {
                Debug.Log("Player 1: Not all ships placed.");
                return;
            }
            EnterPlayer2Placement();
        }
        else if (playerIndex == 2 && State == TurnState.Player2Placement)
        {
            if (player2Spawner != null && !player2Spawner.AllShipsPlaced())
            {
                Debug.Log("Player 2: Not all ships placed.");
                return;
            }
            EnterBattle();
        }
    }
}

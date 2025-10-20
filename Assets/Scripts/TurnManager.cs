using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum TurnState { Player1Placement, Player2Placement, Battle }

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

    // 1 = player1's turn, 2 = player2's turn
    public int currentBattlePlayer = 1;

    // Score tracking
    public int player1Score = 0;
    public int player2Score = 0;
    public bool gameOver = false;

    void Start()
    {
        // Hook up ready buttons
        if (player1ReadyButton != null) player1ReadyButton.onClick.AddListener(() => OnPlayerReady(1));
        if (player2ReadyButton != null) player2ReadyButton.onClick.AddListener(() => OnPlayerReady(2));

        EnterPlayer1Placement();
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
        // spawn for player 2
        if (player2Spawner != null) player2Spawner.SpawnAll();

        SetPlacementEnabled(player1Grid, player1Spawner, false);
        SetPlacementEnabled(player2Grid, player2Spawner, true);

        UpdateReadyButtons();
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
        }
    }

    // Called when a cell is clicked on player 1's grid (player 2 fires)
    void OnPlayer1GridCellClicked(Vector2Int coord)
    {
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

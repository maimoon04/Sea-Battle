using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;

public enum TurnState { Player1Placement, Player2Placement, Battle }
public enum GameMode { PvP, PvAI }

// Add a helper class to manage ship visibility

public class TurnManager : MonoBehaviour
{
    [Header("Player A (first)")]
    public ShipSpawner player1Spawner;
    public GridController player1Grid;
    public Button player1ReadyButton;
    public Button player1RandomPlaceButton; // New button for random placement
    public CanvasGroup player1GridCanvasGroup; // For fading
    public CannonController player1Cannon;

    [Header("Player B (second)")]
    public ShipSpawner player2Spawner;
    public GridController player2Grid;
    public Button player2ReadyButton;
    public Button player2RandomPlaceButton; // New button for random placement
    public CanvasGroup player2GridCanvasGroup; // For fading
    public CannonController player2Cannon;
    [Header("Ship Status UI")]
    // Single container that will hold two rows: Row1 (player1) and Row2 (player2)
    public RectTransform row1;
    public RectTransform row2;
    public GameObject shipIconPrefab; // prefab containing an Image component used as an icon
    public TextMeshProUGUI scoreText;
     public TextMeshProUGUI gameOverText;
    public GameObject gameOverPanel;
    public GameObject ScoringPanel;
    public TurnState State { get; private set; } = TurnState.Player1Placement;
    public GameMode gameMode = GameMode.PvP;

   public bool isFiringTurn = false;
    // 1 = player1's turn, 2 = player2's turn
    public int currentBattlePlayer = 1;
    // map runtime Ship -> UI Image icon for status display
    private Dictionary<Ship, Image> shipIconMap = new Dictionary<Ship, Image>();

    void Update()
    {
        if (State != TurnState.Battle)
        {
            // Continuously check if all ships are placed and update ready button visibility
            UpdateReadyButtons();
        }
    }

    // Score tracking
    public int player1Score = 0;
    public int player2Score = 0;
    public bool gameOver = false;
    public bool player1Ready = false;
    public bool player2Ready = false;
    public void StartGame(bool isAIMode)
    {
        gameMode = isAIMode ? GameMode.PvAI : GameMode.PvP;
        
        // Hook up ready buttons
        if (player1ReadyButton != null) 
        {
            player1ReadyButton.onClick.AddListener(() => OnPlayerReady(1));
            player1ReadyButton.gameObject.SetActive(false); // Start hidden until ships placed
        }
        if (player2ReadyButton != null) 
        {
            player2ReadyButton.onClick.AddListener(() => OnPlayerReady(2));
            player2ReadyButton.gameObject.SetActive(false); // Start hidden until ships placed
        }

        // Hook up random placement buttons
        if (player1RandomPlaceButton != null) 
            player1RandomPlaceButton.onClick.AddListener(() => OnRandomPlacement(1));
        if (player2RandomPlaceButton != null)
            player2RandomPlaceButton.onClick.AddListener(() => OnRandomPlacement(2));

        // Start the game
        EnterPlayer1Placement();
    }

    void EnterPlayer1Placement()
    {
        State = TurnState.Player1Placement;
        
        // Spawn ships for player 1
        if (player1Spawner != null)
        {
            // Clear any existing ships
            player1Spawner.SpawnAll();
          
        }

        // Enable player1 grid and spawner UI, disable player2
        SetPlacementEnabled(player1Grid, player1Spawner, true);
        SetPlacementEnabled(player2Grid, player2Spawner, false);

        // Show random placement button for player 1, hide for player 2
        if (player1RandomPlaceButton != null)
            player1RandomPlaceButton.gameObject.SetActive(true);
        if (player2RandomPlaceButton != null)
            player2RandomPlaceButton.gameObject.SetActive(false);

        // Fade the enemy grid
        if (player2GridCanvasGroup != null)
            player2GridCanvasGroup.alpha = 0.3f;
        if (player1GridCanvasGroup != null)
            player1GridCanvasGroup.alpha = 1f;

        // Hide ready button until ships are placed
        if (player1ReadyButton != null)
            player1ReadyButton.gameObject.SetActive(false);

       
    }

    void EnterPlayer2Placement()
    {
        State = TurnState.Player2Placement;
        
        if (player2Spawner != null)
        {
           
            player2Spawner.SpawnAll();
        }
        
        SetPlacementEnabled(player1Grid, player1Spawner, false);
        SetPlacementEnabled(player2Grid, player2Spawner, true);
        
        // Show/hide random placement buttons
        if (player1RandomPlaceButton != null)
            player1RandomPlaceButton.gameObject.SetActive(false);
        if (player2RandomPlaceButton != null && gameMode != GameMode.PvAI)
            player2RandomPlaceButton.gameObject.SetActive(true);

        // Fade grids appropriately
        if (player1GridCanvasGroup != null)
            player1GridCanvasGroup.alpha = 0.3f;
        if (player2GridCanvasGroup != null)
            player2GridCanvasGroup.alpha = 1f;

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
            // Hide ready button until ships are placed
            if (player2ReadyButton != null)
                player2ReadyButton.gameObject.SetActive(false); 
           
        }
    }

    // Handle random placement button clicks
    void OnRandomPlacement(int playerIndex)
    {
        if (playerIndex == 1)
        {
            // Clear any manually placed ships first
            if (player1Spawner != null)
            {
                foreach (var ship in player1Spawner.spawnedShips)
                {
                    if (ship.isPlaced)
                        ship.RemoveFromGrid();
                }
                PlaceShipsRandomly(player1Spawner, player1Grid);
                
                // Show ready button since all ships are now placed
                if (player1ReadyButton != null)
                    player1ReadyButton.gameObject.SetActive(true);
            }
        }
        else if (playerIndex == 2 && gameMode != GameMode.PvAI)
        {
            if (player2Spawner != null)
            {
                foreach (var ship in player2Spawner.spawnedShips)
                {
                    if (ship.isPlaced)
                        ship.RemoveFromGrid();
                }
                PlaceShipsRandomly(player2Spawner, player2Grid);
                // Show ready button since all ships are now placed
                if (player2ReadyButton != null)
                    player2ReadyButton.gameObject.SetActive(true);
            }
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
                
                int maxX =  grid.columns - 1;
                int maxY =  grid.rows - ship.shipData.length;
                int x = rand.Next(0, maxX + 1);
                int y = rand.Next(0, maxY + 1);
                Vector2Int start = new Vector2Int(x, y);
               
                placed = grid.TryPlaceShip(ship, start, false);
                attempts++;
            }
        }
        spawner.TurnOffAllShips();
    }

    void EnterBattle()
    {
        State = TurnState.Battle;
        player1Spawner.container.gameObject.SetActive(false);
        row1.parent.gameObject.SetActive(true);
    // both grids active for firing turns; spawners disabled
        SetPlacementEnabled(player1Grid, player1Spawner, false);
        SetPlacementEnabled(player2Grid, player2Spawner, false);

        // Hide placement buttons
        if (player1RandomPlaceButton != null)
            player1RandomPlaceButton.gameObject.SetActive(false);
        if (player2RandomPlaceButton != null)
            player2RandomPlaceButton.gameObject.SetActive(false);

        player1ReadyButton.gameObject.SetActive(false);
        player2ReadyButton.gameObject.SetActive(false);

        // Restore full visibility to both grids
        if (player1GridCanvasGroup != null)
            player1GridCanvasGroup.alpha = 1f;
        if (player2GridCanvasGroup != null)
            player2GridCanvasGroup.alpha = 1f;
        ScoringPanel.SetActive(true);
    // Populate ship status UI (one row per player)
    PopulateShipStatusUI();
       
        // Set up initial ship visibility
        ShipVisibilityManager.UpdateShipVisibility(player1Spawner, true); // Player 1 starts
        ShipVisibilityManager.UpdateShipVisibility(player2Spawner, false);

      

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
        if(isFiringTurn) return;
        if (player1Cannon != null && player2Grid != null)
        {
            player1Cannon.targetGrid = player2Grid;
            player1Cannon.shipSpawner = player2Spawner;
            isFiringTurn = true;
            // Fire and wait for completion before starting AI turn
            player1Cannon.FireAtCell(coord, (turn) => {
                NextBattleTurn(turn);
                // Only start AI turn after player's shot is complete
            });
        }
    }

    // Called when a cell is clicked on player 1's grid (player 2 fires)
    void OnPlayer1GridCellClicked(Vector2Int coord)
    {

        if (gameMode == GameMode.PvAI) return; // AI never clicks
        if (State == TurnState.Battle && currentBattlePlayer == 1) return;
        if (gameOver) return;
        if(isFiringTurn) return;
        if (player2Cannon != null && player1Grid != null)
        {
            player2Cannon.targetGrid = player1Grid;
            player2Cannon.shipSpawner = player1Spawner;
            isFiringTurn = true;
            player2Cannon.FireAtCell(coord,  NextBattleTurn);
        }
    }
    // AI fires at a random valid cell on player 1's grid
    IEnumerator AIFireCoroutine()
    {
        // Add a natural delay before AI starts to think
        yield return new WaitForSeconds(1.5f);

        // Find valid targets
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
            // Select target
            var rand = new System.Random();
            var target = validTargets[rand.Next(validTargets.Count)];
            
            // Setup AI cannon
            player2Cannon.targetGrid = player1Grid;
            player2Cannon.shipSpawner = player1Spawner;
            
            // Fire and handle turn completion
            player2Cannon.FireAtCell(target, (turn) => {
                NextBattleTurn(turn);
            });
        }
    }
    // Called by CannonController when a ship is sunk
    public void OnShipSunk(GridController defenderGrid, Ship sunkShip)
    {
        if (gameOver) return;
        // Determine which player is the attacker and which is the defender
        bool defenderIsPlayer1 = (defenderGrid == player1Grid);
        
        // Make the sunk ship visible
        if (sunkShip != null && sunkShip.image != null)
        {
            sunkShip.OnShipSunk(); // Always show sunk ships
            // Update the corresponding UI icon to sunk sprite
            UpdateShipIcon(sunkShip);
        }
        
        if (!defenderIsPlayer1)
        {
            // Player 1 sunk a ship
            player1Score++;
            Debug.Log($"Player 1 sunk a ship! Score: {player1Score}");
            if (AllShipsSunk(player2Spawner))
            {
                gameOver = true;
                scoreText.text = $"<color=#0077ff>Player 1</color> wins!";
                // Show all ships when game is over
                ShipVisibilityManager.UpdateShipVisibility(player1Spawner, true);
                ShipVisibilityManager.UpdateShipVisibility(player2Spawner, true);
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
                scoreText.text = $"<color=#ff3333>Player 2</color> wins!";
                // Show all ships when game is over
                ShipVisibilityManager.UpdateShipVisibility(player1Spawner, true);
                ShipVisibilityManager.UpdateShipVisibility(player2Spawner, true);
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
       gameOverPanel.SetActive(true);
       gameOverText.text = $"Player {winner} Wins!";
        Debug.Log($"Game Over! Player {winner} wins.");
    }

    void NextBattleTurn(bool isSamePlayerTurn)
    {
        isFiringTurn = false;
        Debug.Log("NextBattleTurn called. isSamePlayerTurn: " + isSamePlayerTurn);
        if(isSamePlayerTurn)
        {
            UpdateBattleUI();
            if (currentBattlePlayer == 1)
            {
                 currentBattlePlayer = 1; 
            }   
            else
            {
            if (gameMode == GameMode.PvAI && !gameOver)
                {
                    StartCoroutine(AIFireCoroutine());
                }
            }
            return;
        }

        // Alternate turn
        currentBattlePlayer = (currentBattlePlayer == 1) ? 2 : 1;
        if (currentBattlePlayer == 2 && gameMode == GameMode.PvAI && !gameOver)
            {
               StartCoroutine(AIFireCoroutine());
            }
        UpdateBattleUI();
    }

    void UpdateBattleUI()
    {
        // Update cannon states and score with colored text
       
        scoreText.text = $"<color=#0077ff>{player1Score}</color>  |  <color=#ff3333> {player2Score}</color>";
        if (player1Cannon != null)
            player1Cannon.cannonballParent.SetActive(currentBattlePlayer == 1);
        if (player2Cannon != null)
            player2Cannon.cannonballParent.SetActive(currentBattlePlayer == 2);

        // Update grid visuals to indicate active turn
        if (player1GridCanvasGroup != null)
            player1GridCanvasGroup.alpha = currentBattlePlayer == 2 ? 1f : 0.7f;
        if (player2GridCanvasGroup != null)
            player2GridCanvasGroup.alpha = currentBattlePlayer == 1 ? 1f : 0.7f;

        // Update ship visibility based on current turn
        ShipVisibilityManager.UpdateShipVisibility(player1Spawner, currentBattlePlayer == 1);
        ShipVisibilityManager.UpdateShipVisibility(player2Spawner, currentBattlePlayer == 2 && gameMode != GameMode.PvAI);
    }

    // Clear any existing ship status UI icons and mapping
    void ClearShipStatusUI()
    {
        shipIconMap.Clear();

        ClearChildren(row1);
        ClearChildren(row2);
    }
    void ClearChildren(RectTransform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    // Instantiate icons for each player's spawned ships and cache mapping
    void PopulateShipStatusUI()
    {
        // Clear existing first
        ClearShipStatusUI();

        // Helper to create icons for a spawner in a container
        void CreateIconsFor(ShipSpawner spawner, RectTransform container)
        {
            if (spawner == null || container == null || shipIconPrefab == null) return;
            foreach (var ship in spawner.spawnedShips)
            {
                if (ship == null) continue;
                GameObject go = Instantiate(shipIconPrefab, container);
                go.transform.localScale = Vector3.one;
                Image img = go.GetComponent<Image>();
                if (img == null)
                    img = go.GetComponentInChildren<Image>();
                if (img != null)
                {
                    Sprite s = (ship.shipData != null && ship.shipData.shipSprite != null) ? ship.shipData.shipSprite : null;
                    img.sprite = s;
                    img.preserveAspect = true;
                }
                // store mapping for runtime updates
                if (!shipIconMap.ContainsKey(ship) && img != null)
                    shipIconMap[ship] = img;
            }
        }

        // Ensure parent has a vertical layout so rows stack; add if missing
        
            var v = row1.parent.GetComponent<VerticalLayoutGroup>();
            v.childControlHeight = false;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            v.spacing = 4f;
        

        // Use two rows under the single container: Row1 = player1, Row2 = player2
        RectTransform rowA = GetOrCreateRow(row1);
        RectTransform rowB = GetOrCreateRow(row2);
        CreateIconsFor(player1Spawner, rowA);
        CreateIconsFor(player2Spawner, rowB);
    }

    // Update the UI icon for a specific ship (e.g., when it is sunk)
    void UpdateShipIcon(Ship ship)
    {
        if (ship == null) return;
        if (shipIconMap.TryGetValue(ship, out Image img))
        {
            Sprite sunkSprite = (ship.shipData != null && ship.shipData.SunkShipSprite != null) ? ship.shipData.SunkShipSprite : null;
            if (sunkSprite != null)
                img.sprite = sunkSprite;
            // visually indicate sunk (dim or tint)
            img.color = Color.gray;
        }
    }

    // Ensure the parent has two row children. Creates a child GameObject with HorizontalLayoutGroup if missing.
    RectTransform GetOrCreateRow(RectTransform parent)
    {
  
      
        parent.localScale = Vector3.one;
        // stretch horizontally
        parent.anchorMin = new Vector2(0, 0.5f);
        parent.anchorMax = new Vector2(1, 0.5f);
        parent.pivot = new Vector2(0.5f, 0.5f);
        parent.sizeDelta = new Vector2(0, 0);

        return parent;
    }


    void UpdateReadyButtons()
    {
        // Only show ready buttons during placement phase AND when all ships are placed
        if (!player1Ready )
        {
            bool allShipsPlaced = (player1Spawner != null && player1Spawner.AllShipsPlaced());
            if (player1ReadyButton != null)
                player1ReadyButton.gameObject.SetActive(allShipsPlaced);
                
            if(allShipsPlaced)
                player1Spawner.TurnOffAllShips();
        }
        else if (!player2Ready &&  gameMode != GameMode.PvAI)
        {
            bool allShipsPlaced = (player2Spawner != null && player2Spawner.AllShipsPlaced());
            if (player2ReadyButton != null)
                player2ReadyButton.gameObject.SetActive(allShipsPlaced);

            if(allShipsPlaced)
                player2Spawner.TurnOffAllShips();
        }
    }

    void OnPlayerReady(int playerIndex)
    {
        if (playerIndex == 1 && State == TurnState.Player1Placement)
        {
            player1Ready = true;
            player1ReadyButton.gameObject.SetActive(false);
            ShipVisibilityManager.UpdateShipVisibility(player1Spawner, false);
            if (player1Spawner != null && !player1Spawner.AllShipsPlaced())
            {
                Debug.Log("Player 1: Not all ships placed.");
                return;
            }
            EnterPlayer2Placement();
        }
        else if (playerIndex == 2 && State == TurnState.Player2Placement)
        {
            player2Ready = true;
            player2ReadyButton.gameObject.SetActive(false);
            ShipVisibilityManager.UpdateShipVisibility(player2Spawner, false);
            if (player2Spawner != null && !player2Spawner.AllShipsPlaced())
            {
                Debug.Log("Player 2: Not all ships placed.");
                return;
            }
            EnterBattle();
        }
    }
}

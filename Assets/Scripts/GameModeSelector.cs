using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameModeSelector : MonoBehaviour
{
    [Header("UI References")]
    public GameObject selectionPanel;
    public GameObject countdownPanel;
    public TMP_Text countdownText;
    
    [Header("Game Setup")]
    public TurnManager turnManager;
    
    private void Start()
    {
        // Show selection panel, hide countdown
        if (selectionPanel) selectionPanel.SetActive(true);
        if (countdownPanel) countdownPanel.SetActive(false);
    }

    public void SelectPvPMode()
    {
        StartCoroutine(StartGameWithCountdown(false));
    }

    public void SelectAIMode()
    {
        StartCoroutine(StartGameWithCountdown(true));
    }

    private IEnumerator StartGameWithCountdown(bool isAIMode)
    {
        // Hide selection panel, show countdown
        if (selectionPanel) selectionPanel.SetActive(false);
        if (countdownPanel) countdownPanel.SetActive(true);

        // Countdown from 3
        for (int i = 3; i > 0; i--)
        {
            if (countdownText) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        // Hide countdown panel
        if (countdownPanel) countdownPanel.SetActive(false);

        // Start the game
        if (turnManager != null)
        {
            turnManager.StartGame(isAIMode);
        }
    }
}
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Fusion;
using System.Linq;
using JetBrains.Annotations;
using System.Collections;

public class GameScoreboardUI : MonoBehaviour
{
    public static GameScoreboardUI Instance { get; private set; }

    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject scoreItemPrefab;

  
    [System.Serializable]
    public class ScoreEntry
    {
         
        public PlayerNetwork Player;
        public string Name;
        public int Score;
        public GameObject Row;
        public TextMeshProUGUI NameField;
        public TextMeshProUGUI ScoreField;
    }

    // Public list of entries (name + score + row) created/updated in RefreshScores
    public List<ScoreEntry> scoreEntries = new List<ScoreEntry>();

    private void Awake() => Instance = this;

    public IEnumerator Start()
    {
        yield return new WaitUntil(() => ShapesManager.Instance.playerNetwork != null && NetworkManagerFusion.Instance?.GetRunner() != null);
        RefreshScores();
    }

    public void RefreshScores()
    {
        Debug.Log("[GameScoreboardUI] RefreshScores called");
        if (contentParent == null || scoreItemPrefab == null) 
        {
            Debug.LogWarning("[GameScoreboardUI] Missing required references: contentParent or scoreItemPrefab");
            return;
        }
        
        // Get the Fusion network runner
        var runner = NetworkManagerFusion.Instance?.GetRunner();
        if (runner == null)
        {
            Debug.LogError("[GameScoreboardUI] No active NetworkRunner found");
            return;
        }

        // Track which PlayerRefs are currently connected
        var activeRefs = runner.ActivePlayers;
        Debug.Log($"[GameScoreboardUI] Found {activeRefs.Count()} active players");

        // Clear any existing UI rows
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            var child = contentParent.GetChild(i).gameObject;
            Destroy(child);
        }
        scoreEntries.Clear();
        Debug.Log("[GameScoreboardUI] Cleared existing scoreboard entries");
        // Create rows for current networked players
        PlayerNetwork[] playerNetworks = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);

        foreach (var playerRef in playerNetworks)
        {
            Debug.Log($"[GameScoreboardUI] Processing player {playerRef}");

            Debug.Log($"[GameScoreboardUI] Found valid PlayerNetwork for {playerRef}");

            var rowGO = Instantiate(scoreItemPrefab, contentParent);
            var nameStr = playerRef.DisplayName.ToString();
            var scoreVal = playerRef.Score;

            // Find TMP fields by child name hints first, then fallback to first/second text components.
            var allTexts = rowGO.GetComponentsInChildren<TextMeshProUGUI>(true);
            allTexts[0].text = nameStr;
            allTexts[1].text = scoreVal.ToString();
 
            Debug.Log("texts all assigned");

            var entry = new ScoreEntry { Player = playerRef, Name = nameStr, Score = scoreVal, Row = rowGO, NameField = allTexts[0], ScoreField = allTexts[1] };
            scoreEntries.Add(entry);

            


            Debug.Log($"[GameScoreboardUI] Added score entry for {nameStr} with score {scoreVal}");
        }
    }

    public void UpdateScore()
    {
        foreach(var score in scoreEntries)
        {
            if (score.ScoreField != null)
            {
                score.Score = score.Player.Score;
                score.ScoreField.text = score.Player.Score.ToString();
            }
        }
    }
}

using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameTimerManager : NetworkBehaviour
{
    [Networked] public float RemainingTime { get; set; }
    [Networked] public bool TimerRunning { get; set; }

    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    
    [SerializeField] private TextMeshProUGUI winnerScore;

    public override void Spawned()
    {
        // Any [Networked] fields may be read/assigned now
        if (Object.HasStateAuthority)
        {
            TimerRunning = false; // Timer only started by match start method
            RemainingTime = 0;
        }

        UpdateTimerText();
    }

    public void InitializeTimer(float matchTime)
    {
        if (Object != null && Object.HasStateAuthority)
        {
            RemainingTime = matchTime;
            TimerRunning = true;
        }
    }

    void Update()
    {
        //  Always guard all [Networked] access!
        if (Object == null)
            return;

        if (TimerRunning)
        {
            if (Object.HasStateAuthority)
            {
                RemainingTime -= Time.deltaTime;
                if (RemainingTime <= 0)
                {
                    RemainingTime = 0;
                    TimerRunning = false;
                    RPC_EndMatch();
                }
            }

            UpdateTimerText();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EndMatch()
    {
        TimerRunning = false;
        ShowResults();
    }

    private void UpdateTimerText()
    {
        if (timerText)
            timerText.text = $"{Mathf.CeilToInt(RemainingTime)}s";
    }

    private void ShowResults()
    {
        ShapesManager.Instance.spawnedParent.gameObject.SetActive(false);

        if (resultPanel) resultPanel.SetActive(true);

        // Get all PlayerNetwork components, order by Score
        var players = FindObjectsOfType<PlayerNetwork>()
            .OrderByDescending(p => p.Score)
            .ToList();

        if (players.Count > 0)
            winnerText.text = players[0].DisplayName.ToString();
            winnerScore.text = players[0].Score.ToString();

    }
}

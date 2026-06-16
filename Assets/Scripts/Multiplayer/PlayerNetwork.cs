using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PlayerNetwork : NetworkBehaviour
{
    [Networked, Capacity(32)]
    public NetworkString<_32> DisplayName { get; set; }

    [Networked]
    public int Score { get; set; }

    public bool IsLocal => Object != null && Object.HasInputAuthority;

    // Static dictionary to hold all player instances on host, keyed by DisplayName
    private static Dictionary<string, PlayerNetwork> s_PlayerLookup = new Dictionary<string, PlayerNetwork>();

    #region Player Spawn/Despawn
    public override void Spawned()
    {
        // Only host/state authority manages the dictionary
        if (Object.HasStateAuthority)
        {
            string nameKey = "";
            try { nameKey = DisplayName.ToString(); } catch { nameKey = ""; }
            if (!string.IsNullOrEmpty(nameKey) && !s_PlayerLookup.ContainsKey(nameKey))
            {
                s_PlayerLookup.Add(nameKey, this);
            }
        }

        if (IsLocal)
        {
            var nm = FindObjectOfType<NetworkManagerFusion>();
            if (nm != null && !string.IsNullOrEmpty(nm.LocalPlayerName))
            {
                DisplayName = nm.LocalPlayerName;
                Debug.Log($"[PlayerNetwork] Setting local player name to: {nm.LocalPlayerName}");
                // Tell host to update networked DisplayName
                RPC_SetDisplayNameOnHost(nm.LocalPlayerName);
            }

            // Register with ShapesManager if present (may be null if game scene not loaded yet)
            if (ShapesManager.Instance != null)
            {
                ShapesManager.Instance.playerNetwork = this;
            }

            // Always refresh scoreboard UI when spawned
            if (GameScoreboardUI.Instance != null)
            {
                Debug.Log("[PlayerNetwork] Refreshing scoreboard on spawn");
                GameScoreboardUI.Instance.RefreshScores();
            }
        }

        LobbyUI.Instance?.RefreshPlayerList();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Object != null && Object.HasStateAuthority)
        {
            string nameKey = "";
            try { nameKey = DisplayName.ToString(); } catch { nameKey = ""; }
            if (!string.IsNullOrEmpty(nameKey) && s_PlayerLookup.ContainsKey(nameKey))
                s_PlayerLookup.Remove(nameKey);
        }

        LobbyUI.Instance?.RefreshPlayerList();
    }
    #endregion

    #region DisplayName RPCs
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetDisplayNameOnHost(string newName, RpcInfo info = default)
    {
        DisplayName = string.IsNullOrEmpty(newName) ? "Player" : newName;

        // Update host dictionary
        if (Object.HasStateAuthority)
        {
            if (!s_PlayerLookup.ContainsKey(DisplayName.ToString()))
            {
                s_PlayerLookup.Add(DisplayName.ToString(), this);
            }
        }

        // Replicate to all clients
        RPC_ReplicateDisplayName(DisplayName.ToString());
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ReplicateDisplayName(string newName)
    {
        DisplayName = newName;
        LobbyUI.Instance?.RefreshPlayerList();
        GameScoreboardUI.Instance?.RefreshScores(); // Add refresh here to catch name changes
    }
    #endregion

    #region Score Handling
    // Called by local player to add score
    public void AddScore(int amount)
    {
        if (IsLocal)
        {
            // Send RPC to host/state authority using DisplayName
            RPC_ReportScore(DisplayName.ToString(), amount);

        }

    }

    // Runs on host/state authority
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ReportScore(string playerName, int amount)
    {
        if (string.IsNullOrEmpty(playerName)) return;

        // Lookup player instance using DisplayName
        if (s_PlayerLookup.TryGetValue(playerName, out PlayerNetwork player))
        {
            player.Score = amount; // Networked → auto sync to all clients
            Debug.Log($"[PlayerNetwork] Updated score for {playerName} to {player.Score}");
            
            // Notify all clients to update their scoreboards
            RPC_NotifyScoreChanged();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyScoreChanged()
    {
        // Update the scoreboard on all clients when scores change
        if (GameScoreboardUI.Instance != null)
        {
            Debug.Log("[PlayerNetwork] Refreshing scoreboard after score change");
            GameScoreboardUI.Instance.RefreshScores();
        }

    }
    #endregion
}

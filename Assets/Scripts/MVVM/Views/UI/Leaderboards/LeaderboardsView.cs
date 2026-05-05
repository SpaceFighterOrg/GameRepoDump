using Assets.Scripts.Utils.I18N;
using Backend.Common.DTOs.Leaderboard;
using GenericEventSystem.EventData;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class LeaderboardsView : MonoBehaviour
{
    private static readonly Dictionary<int, string> FactionPrefixes = new()
    {
        { 1, "coalition" },
        { 2, "cartel" },
        { 3, "mandate" }
    };

    private VisualElement root;

    private void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        root.Q<Label>(className: "faction-name--cartel").text    = LocalisationKeys.FactionCartel.Localize();
        root.Q<Label>(className: "faction-name--coalition").text = LocalisationKeys.FactionCoalition.Localize();
        root.Q<Label>(className: "faction-name--mandate").text   = LocalisationKeys.FactionMandate.Localize();

        foreach (var label in root.Query<Label>(className: "stat-cell-label").ToList())
        {
            label.text = label.text switch
            {
                "KDA" => LocalisationKeys.StatKda.Localize(),
                "ELO" => LocalisationKeys.StatElo.Localize(),
                _     => label.text
            };
        }
    }

    public void HandleLeaderboardsOpened(EventData data)
    {
        StartCoroutine(GetLeaderboards());
    }

    private IEnumerator GetLeaderboards()
    {
        yield return ApiService.Call(
            ApiService.Api.LeaderboardsAsync,
            leaderboards => PopulateLeaderboard(leaderboards),
            ApiService.OnError
        );
    }

    private void PopulateLeaderboard(LeaderboardDTO leaderboards)
    {
        var byFaction = leaderboards.Elements
            .GroupBy(e => e.FactionId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(e => e.ELO).Take(3).ToList()
            );

        foreach (var (factionId, prefix) in FactionPrefixes)
        {
            var players = byFaction.TryGetValue(factionId, out var list) ? list : new();

            for (int rank = 1; rank <= 3; rank++)
            {
                bool hasPlayer = rank <= players.Count;
                var player = hasPlayer ? players[rank - 1] : null;

                SetLabel($"{prefix}-p{rank}-name", hasPlayer ? player.Username : "—");
                SetLabel($"{prefix}-p{rank}-kda", hasPlayer ? player.KDA.ToString("F2") : "—");
                SetLabel($"{prefix}-p{rank}-elo", hasPlayer ? player.ELO.ToString() : "—");
            }
        }
    }

    private void SetLabel(string elementName, string value)
    {
        var label = root.Q<Label>(elementName);
        if (label != null)
            label.text = value;
        else
            Debug.LogWarning($"[LeaderboardsView] Could not find label: '{elementName}'");
    }
}
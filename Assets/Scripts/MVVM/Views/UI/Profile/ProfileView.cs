using Assets.Scripts;
using Assets.Scripts.Utils.I18N;
using Backend.Common.DTOs.Profile;
using GenericEventSystem.EventData;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ProfileView : MonoBehaviour
{
    private VisualElement _root;

    private Label _usernameLabel;
    private Label _levelLabel;
    private Label _eloLabel;

    private Label _killsLabel;
    private Label _deathsLabel;
    private Label _assistsLabel;

    private Label _gamesPlayed;
    private Label _gamesWon;
    private Label _factionLabel;

    private Label _planetLabel;

    private Button _closeBtn;

    private void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        _usernameLabel = _root.Q<Label>("username-label");
        _eloLabel      = _root.Q<Label>("elo-label");
        _levelLabel    = _root.Q<Label>("level-label");

        _killsLabel   = _root.Q<Label>("kills-label");
        _deathsLabel  = _root.Q<Label>("deaths-label");
        _assistsLabel = _root.Q<Label>("assists-label");

        _gamesPlayed = _root.Q<Label>("games-played-label");
        _gamesWon    = _root.Q<Label>("games-won-label");

        _factionLabel = _root.Q<Label>("faction-label");
        _planetLabel  = _root.Q<Label>("planet-label");

        _closeBtn = _root.Q<Button>("profile-close-btn");
        _closeBtn.style.display = DisplayStyle.None;

        foreach (var label in _root.Query<Label>(className: "stat-label").ToList())
        {
            label.text = label.text switch
            {
                "LEVEL"        => LocalisationKeys.StatLevel.Localize(),
                "ELO"          => LocalisationKeys.StatElo.Localize(),
                "Kills"        => LocalisationKeys.StatKills.Localize(),
                "Deaths"       => LocalisationKeys.StatDeaths.Localize(),
                "Assists"      => LocalisationKeys.StatAssists.Localize(),
                "Games Played" => LocalisationKeys.StatGamesPlayed.Localize(),
                "Games Won"    => LocalisationKeys.StatGamesWon.Localize(),
                "Faction"      => LocalisationKeys.StatFaction.Localize(),
                "Planet"       => LocalisationKeys.StatPlanet.Localize(),
                _              => label.text
            };
        }
        foreach (var label in _root.Query<Label>(className: "section-header").ToList())
        {
            label.text = label.text switch
            {
                "COMBAT"   => LocalisationKeys.SectionCombat.Localize(),
                "MATCHES"  => LocalisationKeys.SectionMatches.Localize(),
                "LOCATION" => LocalisationKeys.SectionLocation.Localize(),
                _          => label.text
            };
        }

        _root.style.display = DisplayStyle.None;
    }

    public void Open(int userId, bool showBackButton = false)
    {
        _closeBtn.style.display = showBackButton ? DisplayStyle.Flex : DisplayStyle.None;
        if (showBackButton)
            _closeBtn.RegisterCallback<ClickEvent>(_ => NavigationManager.Instance.Pop());

        _root.style.display = DisplayStyle.Flex;
        StartCoroutine(GetProfile(userId));
    }

    public void HandleProfileOpened(EventData data)
    {
        Open(((UserIdEventData)data).UserId);
    }

    private IEnumerator GetProfile(int userId)
    {
        yield return ApiService.Call(
            () => ApiService.Api.ProfileAsync(userId),
            prof =>
            {
                _usernameLabel.text = prof.Username;
                _levelLabel.text = prof.Level.ToString();
                _eloLabel.text = prof.ELO.ToString();

                _killsLabel.text = prof.Kills.ToString();
                _deathsLabel.text = prof.Deaths.ToString();
                _assistsLabel.text = prof.Assists.ToString();

                _gamesPlayed.text = prof.GamesPlayed.ToString();
                _gamesWon.text = prof.GamesWon.ToString();

                _factionLabel.text = prof.FactionName.Localize();
                _planetLabel.text = PlanetTitleConverter.Translate(prof.PlanetLocationName);
            },
            ApiService.OnError);
    }
}

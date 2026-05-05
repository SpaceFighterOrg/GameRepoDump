using Assets.Scripts.Utils;
using Assets.Scripts.Utils.I18N;
using GenericEventSystem;
using GenericEventSystem.EventData;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class HeaderView : MonoBehaviour
{
    [Header("Profile Events")]
    [SerializeField] private EventDefinition _profileOpenEventChannel;

    [Header("Users Events")]
    [SerializeField] private EventDefinition _usersOpenEventChannel;

    [Header("Leaderboards Events")]
    [SerializeField] private EventDefinition _leaderboardsOpenEventChannel;

    [Header("Galaxy Events")]
    [SerializeField] private EventDefinition _galaxyOpenEventChannel;
    [SerializeField] private EventDefinition _galaxyChangedEventChannel;

    [Header("Lanaguage Event")]
    [SerializeField] private EventDefinition _languageChanged;

    private Button _profileButton;
    private Button _friendsButton;
    private Button _leaderboardsButton;
    private Button _galaxyButton;
    private Button _resetGalaxyButton;
    private Button _logoutButton;
    private Button _languageButton;

    private Button _activeToggle;
    private (Button btn, EventDefinition onOpenEvent, Func<EventData> onOpenEventData, string route)[] _toggleGroup;

    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _profileButton      = root.Q<Button>("profile-button");
        _friendsButton      = root.Q<Button>("friends-button");
        _leaderboardsButton = root.Q<Button>("leaderboards-button");
        _galaxyButton       = root.Q<Button>("galaxy-button");
        _resetGalaxyButton  = root.Q<Button>("reset-galaxy-button");
        _logoutButton       = root.Q<Button>("logout-button");
        _languageButton     = root.Q<Button>("language-button");

        ApplyNavLabels();
        SessionManager.OnLanguageChanged += ApplyNavLabels;
        _languageButton.RegisterCallback<ClickEvent>(_ => ToggleLanguage());

        _toggleGroup = new[]
        {
            (_profileButton,      _profileOpenEventChannel,      (Func<EventData>)(() => new UserIdEventData { UserId = SessionManager.UserId }), "ProfileView"),
            (_friendsButton,      _usersOpenEventChannel,      () => new EventData(), "UsersView"),
            (_leaderboardsButton, _leaderboardsOpenEventChannel, () => new EventData(), "LeaderboardsView"),
            (_galaxyButton,       _galaxyOpenEventChannel,       () => new EventData(), "SelectedPlanetView"),
        };

        _activeToggle = _galaxyButton;
        _activeToggle.AddToClassList("header-toggle--active");

        foreach (var entry in _toggleGroup)
        {
            var captured = entry;
            captured.btn.RegisterCallback<ClickEvent>(_ => HandleToggle(captured));
        }

        _resetGalaxyButton.RegisterCallback<ClickEvent>(_ => StartCoroutine(InitializeGalaxy()));
        _logoutButton.RegisterCallback<ClickEvent>(_ => StartCoroutine(Logout()));

        Debug.Log($"RoleId: {SessionManager.RoleId}");
        if (SessionManager.RoleId == 1) 
            _resetGalaxyButton.style.display = DisplayStyle.None;
    }

    private void OnDestroy()
    {
        SessionManager.OnLanguageChanged -= ApplyNavLabels;
    }

    private void ApplyNavLabels()
    {
        _profileButton.text      = LocalisationKeys.NavProfile.Localize();
        _friendsButton.text      = LocalisationKeys.NavUsers.Localize();
        _leaderboardsButton.text = LocalisationKeys.NavLeaderboards.Localize();
        _galaxyButton.text       = LocalisationKeys.NavGalaxy.Localize();
        _languageButton.text     = SessionManager.Language == Languages.English ? "BG" : "EN";
    }

    private void ToggleLanguage()
    {
        var lang = SessionManager.Language == Languages.English
            ? Languages.Bulgarian
            : Languages.English;

        SessionManager.Language = lang;

        _languageChanged.Raise(new LanguageEventData() { Language = lang });
    }

    private void HandleToggle((Button btn, EventDefinition onOpenEvent, Func<EventData> onOpenEventData, string route) entry)
    {
        if(_activeToggle == entry.btn) return;

        if (_activeToggle != null)
        {
            foreach (var e in _toggleGroup)
            {
                if (e.btn != _activeToggle) continue;
                _activeToggle.RemoveFromClassList("header-toggle--active");
                break;
            }
        }

        entry.btn.AddToClassList("header-toggle--active");
        NavigationManager.Instance.Replace(entry.route);
        entry.onOpenEvent.Raise(entry.onOpenEventData());
        _activeToggle = entry.btn;
    }

    private IEnumerator InitializeGalaxy()
    {
        yield return ApiService.Call(() => ApiService.Api.InitializeAsync(null, null, null, null, null, null, null),
            () => { },
            ApiService.OnError);

        yield return ApiService.Call(ApiService.Api.GalaxyAsync,
            galaxy => _galaxyChangedEventChannel.Raise(new GalaxyChangedEventData { Galaxy = galaxy }),
            ApiService.OnError);
    }

    private IEnumerator Logout()
    {
        yield return ApiService.Call(
            ApiService.Api.LogoutAsync,
            () => SessionManager.Expire(),
            ApiService.OnError);
    }
}

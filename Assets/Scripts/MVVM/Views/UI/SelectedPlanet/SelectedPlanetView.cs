using Assets.Scripts.Utils;
using Assets.Scripts.Utils.I18N;
using Backend.Common.DTOs.Map;
using GenericEventSystem;
using GenericEventSystem.EventData;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.MVVM.Views.UI.SelectedPlanet
{
    [RequireComponent(typeof(UIDocument))]
    public class SelectedPlanetView : ViewBase
    {
        [SerializeField] private FactionRegistry _factionRegistry;
        [SerializeField] private EventDefinition _playerShipMovedEventChannel;

        private Label _planetNameLabel;
        private Button _battleButton;
        private Button _lockButton;

        private FactionControlView _factionControlView;
        private int _selectedPlanetId;

        private System.Action<Backend.Common.DTOs.Party.PartyDTO> _onPartyUpdated;

        private void Awake()
        {
            var doc = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            _planetNameLabel = root.Q<Label>("planet-name");
            _battleButton = root.Q<Button>("battle-button");
            _lockButton = root.Q<Button>("lock-button");

            _battleButton?.RegisterCallback<ClickEvent>(_ => OnBattleOrReadyClicked());
            _lockButton?.RegisterCallback<ClickEvent>(_ => OnLockClicked());

            _factionControlView = new FactionControlView(_factionRegistry);
            _factionControlView.BindElements(root);

            _onPartyUpdated = _ => RefreshButtons();
            MatchmakingHubService.OnPartyUpdated += _onPartyUpdated;

            Hide();
        }

        private void OnDestroy()
        {
            MatchmakingHubService.OnPartyUpdated -= _onPartyUpdated;
        }

        private void OnLockClicked()
        {
            if (_selectedPlanetId == 0 || SessionManager.LatestGalaxyPull == null) return;

            var planet = SessionManager.LatestGalaxyPull.Planets.FirstOrDefault(p => p.Id == _selectedPlanetId);
            if (planet == null) return;

            if (planet.FactionId == SessionManager.FactionId)
            {
                SessionManager.AttackingPlanetId = 0;
                StartCoroutine(MoveAndLockDefence(_selectedPlanetId));
                return;
            }
            else
                SessionManager.AttackingPlanetId = _selectedPlanetId; 

            RefreshButtons();
        }

        private IEnumerator MoveAndLockDefence(int planetId)
        {
            yield return ApiService.Call(
                () => ApiService.Api.MoveAsync(planetId),
                () => {
                    SessionManager.LocationPlanetId = planetId;
                    _playerShipMovedEventChannel.Raise(new PlanetIdEventData { PlanetId = planetId });
                    RefreshButtons();
                },
                ApiService.OnError);
        }

        private void OnBattleOrReadyClicked()
        {
            var party = PartyView.Instance;
            bool isInParty = party != null && party.IsInParty;

            if (isInParty && !party.IsLeader)
            {
                MatchmakingHubService.SetReady(!party.IsLocalPlayerReady);
                return;
            }

            StartCoroutine(EnterMatchmakingCoroutine());
        }

        private IEnumerator EnterMatchmakingCoroutine()
        {
            var party = PartyView.Instance;
            if (party != null && !party.IsInParty)
                yield return party.EnsureInParty();

            MatchmakingHubService.EnterMatchmaking(SessionManager.AttackingPlanetId, SessionManager.LocationPlanetId);
        }

        public void Refresh(MapPlanetDTO planet)
        {
            if (planet == null) { Hide(); return; }

            if (_planetNameLabel != null)
                _planetNameLabel.text = PlanetTitleConverter.Translate(planet.Name);

            List<PlanetBattleStatsDTO> dataToBePassed;
            if (planet.BattleStats == null || planet.BattleStats.Count == 0)
                dataToBePassed = new List<PlanetBattleStatsDTO>() { new() { FactionId = planet.FactionId, ControlPoints = 100 } };
            else
                dataToBePassed = planet.BattleStats;

            _factionControlView.Refresh(dataToBePassed);

            Show();
        }

        public void Show() => SetVisible(true);
        public void Hide() => SetVisible(false);

        public void HandlePlanetSelected(EventData data)
        {
            var planetSelectedData = data as PlanetIdEventData;
            _selectedPlanetId = planetSelectedData.PlanetId;

            if (_selectedPlanetId == -1)
            {
                Hide();
                return;
            }

            Show();
            var planet = SessionManager.LatestGalaxyPull.Planets.First(p => p.Id == planetSelectedData.PlanetId);

            RefreshButtons(planet);
            Refresh(planet);
        }

        private void RefreshButtons()
        {
            if (_selectedPlanetId <= 0 || SessionManager.LatestGalaxyPull == null) return;
            var planet = SessionManager.LatestGalaxyPull.Planets.FirstOrDefault(p => p.Id == _selectedPlanetId);
            if (planet != null) RefreshButtons(planet);
        }

        private void RefreshButtons(MapPlanetDTO planet)
        {
            var currentPlanet = SessionManager.LatestGalaxyPull?.Planets
                .FirstOrDefault(p => p.Id == SessionManager.LocationPlanetId);

            bool isOwnFaction = planet.FactionId == SessionManager.FactionId;

            bool bordersCurrentLocation = currentPlanet != null
                && ((currentPlanet.ConnectedPlanetIds != null && currentPlanet.ConnectedPlanetIds.Contains(_selectedPlanetId))
                    || (planet.ConnectedPlanetIds != null && planet.ConnectedPlanetIds.Contains(SessionManager.LocationPlanetId)));

            if (_lockButton != null)
            {
                if (isOwnFaction)
                {
                    _lockButton.text = LocalisationKeys.LockDefenceButton.Localize();
                    _lockButton.SetEnabled(true);
                }
                else
                {
                    _lockButton.text = LocalisationKeys.LockAttackButton.Localize();
                    _lockButton.SetEnabled(bordersCurrentLocation);
                }
            }

            // Battle / Ready button
            bool hasBothTargets = SessionManager.LocationPlanetId != 0 && SessionManager.AttackingPlanetId != 0;

            var party = PartyView.Instance;
            bool isInParty = party != null && party.IsInParty;
            bool isLeader = party != null && party.IsLeader;

            if (_battleButton != null)
            {
                if (isInParty && !isLeader)
                {
                    bool ready = party.IsLocalPlayerReady;
                    _battleButton.text = ready ? "READY ✓" : "READY";
                    _battleButton.SetEnabled(true);
                }
                else if (isInParty && isLeader)
                {
                    _battleButton.text = LocalisationKeys.BattleButton.Localize();
                    _battleButton.SetEnabled(hasBothTargets && party.AreAllMembersReady());
                }
                else
                {
                    _battleButton.text = LocalisationKeys.BattleButton.Localize();
                    _battleButton.SetEnabled(hasBothTargets);
                }
            }
        }

        private void SetVisible(bool visible)
        {
            var doc = GetComponent<UIDocument>();
            if (doc?.rootVisualElement != null)
                doc.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

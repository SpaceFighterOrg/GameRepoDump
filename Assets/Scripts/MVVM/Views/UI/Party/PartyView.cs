using Assets.Scripts.Utils;
using Assets.Scripts.Utils.I18N;
using Backend.Common.DTOs.Matchmaking;
using Backend.Common.DTOs.Party;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.MVVM
{
    public class PartyView : MonoBehaviour
    {
        public static PartyView Instance { get; private set; }

        private VisualElement _partyPanel;
        private VisualElement _membersList;
        private Button _leaveBtn;

        private VisualElement _inviteAnchor;
        private Label _inviteFromLabel;
        private Button _inviteAcceptBtn;
        private Button _inviteDeclineBtn;

        private PartyDTO _currentParty;
        private PartyInviteDTO _pendingInvite;
        private int _cachedElo;

        public bool IsInParty => _currentParty != null;
        public bool IsLeader => _currentParty?.LeaderId == SessionManager.UserId;
        public bool IsLocalPlayerReady => _currentParty?.Members?.FirstOrDefault(m => m.Id == SessionManager.UserId)?.IsReady ?? false;
        public bool AreAllMembersReady() => _currentParty?.Members?.All(m => m.IsReady) ?? false;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var root = GetComponent<UIDocument>().rootVisualElement;

            _partyPanel  = root.Q<VisualElement>("party-panel");
            _membersList = root.Q<VisualElement>("party-members-list");
            _leaveBtn    = root.Q<Button>("party-leave-btn");

            _inviteAnchor      = root.Q<VisualElement>("invite-anchor");
            _inviteFromLabel   = root.Q<Label>("invite-from-label");
            _inviteAcceptBtn   = root.Q<Button>("invite-accept-btn");
            _inviteDeclineBtn  = root.Q<Button>("invite-decline-btn");

            root.Q<Label>(className: "party-panel-title").text  = LocalisationKeys.PartyTitle.Localize();
            root.Q<Label>(className: "invite-panel-title").text = LocalisationKeys.PartyInviteTitle.Localize();
            _leaveBtn.text = LocalisationKeys.LeavePartyButton.Localize();
            _inviteAcceptBtn.text = LocalisationKeys.AcceptButton.Localize();
            _inviteDeclineBtn.text = LocalisationKeys.DeclineButton.Localize();

            _partyPanel.style.display   = DisplayStyle.None;
            _inviteAnchor.style.display = DisplayStyle.None;

            _leaveBtn.RegisterCallback<ClickEvent>(_ => OnLeaveClicked());
            _inviteAcceptBtn.RegisterCallback<ClickEvent>(_ => OnAcceptClicked());
            _inviteDeclineBtn.RegisterCallback<ClickEvent>(_ => OnDeclineClicked());

            MatchmakingHubService.OnPartyUpdated   += OnPartyUpdated;
            MatchmakingHubService.OnPartyDisbanded += OnPartyDisbanded;
            MatchmakingHubService.OnPartyInvited   += OnPartyInvited;
            MatchmakingHubService.OnMatchFound += OnMatchFound;


            SessionManager.OnExpired += OnSessionExpired;
        }

        private void Start()
        {
            StartCoroutine(FetchElo());
        }

        private void OnDestroy()
        {
            MatchmakingHubService.OnPartyUpdated   -= OnPartyUpdated;
            MatchmakingHubService.OnPartyDisbanded -= OnPartyDisbanded;
            MatchmakingHubService.OnPartyInvited   -= OnPartyInvited;
            MatchmakingHubService.OnMatchFound -= OnMatchFound;

            SessionManager.OnExpired -= OnSessionExpired;
        }

        public void InviteToParty(int userId)
        {
            if (IsInParty)
                MatchmakingHubService.InvitePlayer(userId);
            else
                StartCoroutine(CreatePartyThenInvite(userId));
        }

        private void OnPartyUpdated(PartyDTO party)
        {
            if (party.Members == null || !party.Members.Any(m => m.Id == SessionManager.UserId))
            {
                OnPartyDisbanded();
                return;
            }

            _currentParty = party;
            _partyPanel.style.display = DisplayStyle.Flex;
            RebuildMemberList(party);
        }

        private void OnMatchFound(MatchmakeResultDTO result)
        {
            _partyPanel.style.display = DisplayStyle.None;
        }

        private void OnPartyDisbanded()
        {
            _currentParty = null;
            _partyPanel.style.display = DisplayStyle.None;
            _membersList.Clear();
        }

        private void OnPartyInvited(PartyInviteDTO invite)
        {
            _pendingInvite = invite;
            _inviteFromLabel.text = $"{invite.FromUsername} invited you to their party";
            _inviteAnchor.style.display = DisplayStyle.Flex;
        }

        private void OnSessionExpired()
        {
            _currentParty  = null;
            _pendingInvite = null;
            _partyPanel.style.display    = DisplayStyle.None;
            _inviteAnchor.style.display  = DisplayStyle.None;
            _membersList.Clear();
        }

        private void RebuildMemberList(PartyDTO party)
        {
            _membersList.Clear();
            const int maxSlots = 5;
            bool amLeader = party.LeaderId == SessionManager.UserId;

            for (int i = 0; i < maxSlots; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("party-member-row");

                if (i < party.Members.Count)
                {
                    var member = party.Members[i];

                    var nameLabel = new Label(member.Username);
                    nameLabel.AddToClassList("party-member-name");
                    if (member.Id == party.LeaderId)
                        nameLabel.AddToClassList("party-member-name--leader");

                    row.Add(nameLabel);

                    if (amLeader && member.Id != SessionManager.UserId)
                    {
                        var capturedId = member.Id;
                        var kickBtn = new Button { text = "✕" };
                        kickBtn.AddToClassList("party-kick-btn");
                        kickBtn.RegisterCallback<ClickEvent>(_ =>
                            MatchmakingHubService.KickPlayer(capturedId));
                        row.Add(kickBtn);
                    }
                }
                else
                {
                    var emptyLabel = new Label(LocalisationKeys.PartyNameListEmpty.Localize());
                    emptyLabel.AddToClassList("party-member-empty");
                    row.Add(emptyLabel);
                }

                _membersList.Add(row);
            }
        }

        private void OnLeaveClicked()
        {
            MatchmakingHubService.LeaveParty();
            OnPartyDisbanded();
        }

        private void OnAcceptClicked()
        {
            if (_pendingInvite == null) return;
            MatchmakingHubService.AcceptInvite(_pendingInvite.PartyId, _cachedElo);
            HideInvite();
        }

        private void OnDeclineClicked()
        {
            if (_pendingInvite == null) return;
            MatchmakingHubService.DeclineInvite(_pendingInvite.PartyId);
            HideInvite();
        }

        private void HideInvite()
        {
            _pendingInvite = null;
            _inviteAnchor.style.display = DisplayStyle.None;
        }

        private IEnumerator FetchElo()
        {
            yield return ApiService.Call(
                () => ApiService.Api.ProfileAsync(SessionManager.UserId),
                prof => _cachedElo = prof.ELO,
                _ => _cachedElo = 0);
        }

        public IEnumerator EnsureInParty()
        {
            if (IsInParty) yield break;
            if (_cachedElo == 0) yield return FetchElo();

            if (!MatchmakingHubService.IsConnected)
            {
                float deadline = Time.realtimeSinceStartup + 5f;
                yield return new WaitUntil(() =>
                    MatchmakingHubService.IsConnected ||
                    Time.realtimeSinceStartup >= deadline);

                if (!MatchmakingHubService.IsConnected)
                {
                    Debug.LogError("[PartyView] Cannot create party: hub not connected after waiting.");
                    yield break;
                }
            }

            MatchmakingHubService.CreateParty(_cachedElo);
            yield return new WaitUntil(() => IsInParty);
        }

        private IEnumerator CreatePartyThenInvite(int userId)
        {
            if (_cachedElo == 0)
                yield return FetchElo();

            if (!MatchmakingHubService.IsConnected)
            {
                Debug.LogWarning("[PartyView] Cannot create party: hub not connected.");
                yield break;
            }

            MatchmakingHubService.CreateParty(_cachedElo);
            yield return new WaitUntil(() => IsInParty);
            MatchmakingHubService.InvitePlayer(userId);
        }
    }
}

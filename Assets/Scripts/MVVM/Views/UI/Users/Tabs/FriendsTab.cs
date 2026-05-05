using Assets.Scripts.Utils.I18N;
using Backend.Common.DTOs.Util;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class FriendsTab : PagedView<NomenclatureDTO>
{
    private VisualElement _list;
    private Label _errorLabel;
    private MonoBehaviour _runner;
    private Action<int> _onViewProfile;
    private Action<int> _onInviteToParty;

    public void Init(VisualElement root, MonoBehaviour runner, Action<int> onViewProfile, Action<int> onInviteToParty)
    {
        _runner          = runner;
        _onViewProfile   = onViewProfile;
        _onInviteToParty = onInviteToParty;

        _list      = root.Q<VisualElement>("friends-list");
        _prev      = root.Q<Button>("friends-prev");
        _next      = root.Q<Button>("friends-next");
        _pageLabel = root.Q<Label>("friends-page-label");
        _errorLabel = root.Q<Label>("friends-error-label");

        _prev.RegisterCallback<ClickEvent>(_ => ChangePage(-1));
        _next.RegisterCallback<ClickEvent>(_ => ChangePage(+1));
    }

    public void Reload()
    {
        _runner.StartCoroutine(GetFriends());
    }

    private IEnumerator GetFriends()
    {
        ClearError();
        yield return ApiService.Call(
            ()=>ApiService.Api.FriendsGETAsync(1,10),
            friends =>
            {
                _data = friends ?? new PagedResultDTO<NomenclatureDTO>();
                ResetToFirstPage();
                Rebuild();
            },
            ShowError);
    }

    public override void Load()
    {
        Reload();
    }

    protected override void Rebuild()
    {
        _list.Clear();
        foreach (var nomenclature in GetPage(_data.Items, _page))
        {
            var captured = nomenclature;

            var row = new VisualElement();
            row.AddToClassList("friend-row");

            var label = new Label(nomenclature.Name);
            label.AddToClassList("friend-row-name");

            var profileBtn = new Button { text = LocalisationKeys.UsersFriendsTabProfileBtn.Localize() };
            profileBtn.AddToClassList("friend-profile-btn");
            profileBtn.RegisterCallback<ClickEvent>(_ => _onViewProfile?.Invoke(captured.Id));

            var partyBtn = new Button { text = LocalisationKeys.UsersFriendsTabPartyBtn.Localize() };
            partyBtn.AddToClassList("friend-party-btn");
            partyBtn.RegisterCallback<ClickEvent>(_ => _onInviteToParty?.Invoke(captured.Id));

            var unfriendBtn = new Button { text = LocalisationKeys.UsersFriendsTabUnfriendBtn.Localize() };
            unfriendBtn.AddToClassList("friend-unfriend-btn");
            unfriendBtn.RegisterCallback<ClickEvent>(_ => _runner.StartCoroutine(Unfriend(captured.Id)));

            row.Add(label);
            row.Add(profileBtn);
            row.Add(partyBtn);
            row.Add(unfriendBtn);
            _list.Add(row);
        }
    }

    private IEnumerator Unfriend(int friendId)
    {
        ClearError();
        yield return ApiService.Call(
            () => ApiService.Api.UnfriendAsync(friendId),
            () => Reload(),
            ShowError);
    }

    private void ShowError(ErrorResponseDTO error)
    {
        ApiService.OnError(error);
        _errorLabel.text = error.Message;
        _errorLabel.style.display = DisplayStyle.Flex;
    }

    private void ClearError()
    {
        _errorLabel.text = "";
        _errorLabel.style.display = DisplayStyle.None;
    }
}

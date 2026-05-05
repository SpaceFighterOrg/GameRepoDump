using Assets.Scripts.Utils.I18N;
using Backend.Common.DTOs.Util;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class UsersTab : PagedView<PlayerSearchResultDTO>
{
    private TextField _searchField;
    private VisualElement _list;
    private Label _errorLabel;
    private MonoBehaviour _runner;
    private Button _searchButton;

    private string _currentQuery = "";

    public void Init(VisualElement root, MonoBehaviour runner)
    {
        _runner = runner;

        _searchField = root.Q<TextField>("users-search-field");
        _list = root.Q<VisualElement>("users-list");
        _prev = root.Q<Button>("users-prev");
        _next = root.Q<Button>("users-next");
        _pageLabel = root.Q<Label>("users-page-label");
        _errorLabel = root.Q<Label>("users-error-label");
        _searchButton = root.Q<Button>("users-search-btn");

        _pageLabel.text = "1/1";
        _searchButton.text = LocalisationKeys.SearchButton.Localize();
        _searchButton.RegisterCallback<ClickEvent>(_ => OnSearch());
        _prev.RegisterCallback<ClickEvent>(_ => ChangePage(-1));
        _next.RegisterCallback<ClickEvent>(_ => ChangePage(+1));

        _prev.SetEnabled(false);
        _prev.AddToClassList("page-btn--disabled");
        _next.SetEnabled(false);
        _next.AddToClassList("page-btn--disabled");
    }

    private void OnSearch()
    {
        _currentQuery = _searchField.value;
        if(_currentQuery != "")
            Reload();
    }

    public void Reload()
    {
        _runner.StartCoroutine(SearchCoroutine(_currentQuery));
    }

    private IEnumerator SearchCoroutine(string username)
    {
        ClearError();
        yield return ApiService.Call(
            ()=>ApiService.Api.SearchAsync(username, null, null),
            users =>
            {
                _data = users ?? new PagedResultDTO<PlayerSearchResultDTO>();
                ResetToFirstPage();
                Rebuild();
            },
            ShowError
        );
    }

    public override void Load() {}

    protected override void Rebuild()
    {
        _list.Clear();

        if (_data == null || _data.Items.Count == 0)
            return;

        foreach (var player in GetPage(_data.Items, _page))
        {
            var row = new VisualElement();
            row.AddToClassList("friend-row");

            var label = new Label(player.Name);
            label.AddToClassList("friend-row-name");

            var addBtn = new Button { text = "Add" };
            addBtn.AddToClassList("add-friend-btn");

            if (player.HasPendingRequest)
            {
                SetPending(addBtn);
            }
            else
            {
                var captured = player;
                addBtn.RegisterCallback<ClickEvent>(_ => _runner.StartCoroutine(SendFriendRequest(captured, addBtn)));
            }

            row.Add(label);
            row.Add(addBtn);
            _list.Add(row);
        }
    }

    private IEnumerator SendFriendRequest(PlayerSearchResultDTO player, Button btn)
    {
        ClearError();
        yield return ApiService.Call(
            () => ApiService.Api.FriendsPOSTAsync(player.Id),
            () => SetPending(btn),
            ShowError);
    }

    private static void SetPending(Button btn)
    {
        btn.SetEnabled(false);
        btn.AddToClassList("add-friend-btn--pending");
    }

    private void ShowError(ErrorResponseDTO error)
    {
        Debug.LogError($"Search failed [{error.StatusCode}]: {error.Message}");
        _errorLabel.text = error.Message;
        _errorLabel.style.display = DisplayStyle.Flex;
    }

    private void ClearError()
    {
        _errorLabel.text = "";
        _errorLabel.style.display = DisplayStyle.None;
    }
}

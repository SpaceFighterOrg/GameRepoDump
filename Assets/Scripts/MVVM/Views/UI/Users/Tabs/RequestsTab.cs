using Assets.Scripts.Utils.I18N;
using Backend.Common.DTOs.Util;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class RequestsTab : PagedView<NomenclatureDTO>
{
    public Action OnRequestAccepted;

    private VisualElement _list;
    private Label _errorLabel;
    private MonoBehaviour _runner;

    public void Init(VisualElement root, MonoBehaviour runner)
    {
        _runner = runner;

        _list      = root.Q<VisualElement>("requests-list");
        _prev      = root.Q<Button>("requests-prev");
        _next      = root.Q<Button>("requests-next");
        _pageLabel = root.Q<Label>("requests-page-label");

        _errorLabel = root.Q<Label>("requests-error-label");

        var refreshBtn = root.Q<Button>("requests-refresh-btn");
        refreshBtn.text = LocalisationKeys.RefreshButton.Localize();
        refreshBtn.RegisterCallback<ClickEvent>(_ => Reload());

        _prev.RegisterCallback<ClickEvent>(_ => ChangePage(-1));
        _next.RegisterCallback<ClickEvent>(_ => ChangePage(+1));
    }

    public void Reload()
    {
        _runner.StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        ClearError();
        yield return ApiService.Call(
            ()=>ApiService.Api.RequestsAsync(null,null),
            requests =>
            {
                _data = requests ?? new PagedResultDTO<NomenclatureDTO>();
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

        if (_data == null || _data.Items.Count == 0)
            return;

        foreach (var nomenclature in GetPage(_data.Items, _page))
        {
            var row = new VisualElement();
            row.AddToClassList("friend-row");

            var label = new Label(nomenclature.Name);
            label.AddToClassList("friend-row-name");

            var captured = nomenclature.Id;

            var approveBtn = new Button { text = "✓" };
            approveBtn.AddToClassList("approve-btn");
            approveBtn.RegisterCallback<ClickEvent>(_ => _runner.StartCoroutine(ApproveRequest(captured)));

            var denyBtn = new Button { text = "✕" };
            denyBtn.AddToClassList("deny-btn");
            denyBtn.RegisterCallback<ClickEvent>(_ => _runner.StartCoroutine(RejectRequest(captured)));

            row.Add(label);
            row.Add(approveBtn);
            row.Add(denyBtn);
            _list.Add(row);
        }
    }

    private IEnumerator ApproveRequest(int userId)
    {
        ClearError();
        yield return ApiService.Call(
            () => ApiService.Api.AcceptAsync(userId),
            () => { Reload(); OnRequestAccepted?.Invoke(); },
            ShowError);
    }

    private IEnumerator RejectRequest(int userId)
    {
        ClearError();
        yield return ApiService.Call(
            () => ApiService.Api.RejectAsync(userId),
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

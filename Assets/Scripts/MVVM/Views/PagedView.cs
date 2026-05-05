using Backend.Common.DTOs.Util;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class PagedView<T>
{
    private const int PageSize = 10;

    protected Button _prev;
    protected Button _next;
    protected Label _pageLabel;

    protected int _page;
    protected PagedResultDTO<T> _data;

    protected static List<T> GetPage(List<T> data, int page)
    {
        int start = page * PageSize;
        int count = Mathf.Min(PageSize, data.Count - start);
        return count > 0 ? data.GetRange(start, count) : new List<T>();
    }

    protected void ChangePage(int delta)
    {
        int maxPage = Mathf.Max(0, (_data.Items.Count - 1) / PageSize);
        _page = Mathf.Clamp(_page + delta, 0, maxPage);
        _pageLabel.text = $"{_page + 1}/{maxPage + 1}";
        UpdatePaginationButtons();
        Rebuild();
    }

    protected void ResetToFirstPage()
    {
        _page = 0;
        _pageLabel.text = $"1/{Mathf.Max(1, (_data.Items.Count - 1) / PageSize + 1)}";
        UpdatePaginationButtons();
    }

    private void UpdatePaginationButtons()
    {
        int maxPage = _data?.Items != null
            ? Mathf.Max(0, (_data.Items.Count - 1) / PageSize)
            : 0;

        bool atFirst = _page <= 0;
        bool atLast  = _page >= maxPage;

        SetPageButton(_prev, atFirst);
        SetPageButton(_next, atLast);
    }

    private static void SetPageButton(Button btn, bool disabled)
    {
        btn.SetEnabled(!disabled);
        if (disabled)
            btn.AddToClassList("page-btn--disabled");
        else
            btn.RemoveFromClassList("page-btn--disabled");
    }

    protected abstract void Rebuild();
    public abstract void Load();
}

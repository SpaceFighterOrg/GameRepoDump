using Assets.Scripts.Utils.I18N;
using GenericEventSystem.EventData;
using UnityEngine;
using UnityEngine.UIElements;

public class UsersView : MonoBehaviour
{

    private Button _tabFriends;
    private Button _tabUsers;
    private Button _tabRequests;
    private Button _activeTab;

    private VisualElement _pageFriends;
    private VisualElement _pageUsers;
    private VisualElement _pageRequests;

    private readonly FriendsTab  _friendsTab  = new();
    private readonly UsersTab    _usersTab    = new();
    private readonly RequestsTab _requestsTab = new();

    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _tabFriends  = root.Q<Button>("tab-friends");
        _tabUsers    = root.Q<Button>("tab-users");
        _tabRequests = root.Q<Button>("tab-requests");

        _tabFriends.text  = LocalisationKeys.TabFriends.Localize();
        _tabUsers.text    = LocalisationKeys.TabUsers.Localize();
        _tabRequests.text = LocalisationKeys.TabRequests.Localize();

        _pageFriends  = root.Q<VisualElement>("page-friends");
        _pageUsers    = root.Q<VisualElement>("page-users");
        _pageRequests = root.Q<VisualElement>("page-requests");

        _friendsTab.Init(_pageFriends,  this, OnViewProfile, OnPartyInvite);
        _usersTab.Init(_pageUsers,      this);
        _requestsTab.Init(_pageRequests, this);

        _requestsTab.OnRequestAccepted = () => _friendsTab.Reload();

        _tabFriends.RegisterCallback<ClickEvent>(_  => { SwitchTab(_tabFriends,  _pageFriends);  _friendsTab.Reload(); });
        _tabUsers.RegisterCallback<ClickEvent>(_    => { SwitchTab(_tabUsers,    _pageUsers); });
        _tabRequests.RegisterCallback<ClickEvent>(_ => { SwitchTab(_tabRequests, _pageRequests); _requestsTab.Reload(); });

        _activeTab = _tabFriends;
    }

    private void Start()
    {
        _friendsTab.Load();
    }

    public void HandleFriendsOpened(EventData data)
    {
        GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.Flex;
        SwitchTab(_tabFriends, _pageFriends);
        _friendsTab.Load();
    }

    public void HandleFriendsClosed(EventData data)
    {
        GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.None;
    }

    private void SwitchTab(Button tab, VisualElement page)
    {
        if (_activeTab != null)
            _activeTab.RemoveFromClassList("friends-tab--active");

        _pageFriends.style.display  = DisplayStyle.None;
        _pageUsers.style.display    = DisplayStyle.None;
        _pageRequests.style.display = DisplayStyle.None;

        tab.AddToClassList("friends-tab--active");
        page.style.display = DisplayStyle.Flex;
        _activeTab = tab;
    }

    private void OnViewProfile(int userId)
    {
        NavigationManager.Instance.Push("ProfileView");
        NavigationManager.Instance.Current.GetComponent<ProfileView>().Open(userId, showBackButton: true);
    }

    private void OnPartyInvite(int userId)
    {
        Assets.Scripts.MVVM.PartyView.Instance?.InviteToParty(userId);
    }
}

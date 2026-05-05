using Assets.Scripts.MVVM.ViewModels;
using Assets.Scripts.Utils.I18N;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine;
using GenericEventSystem;
using Assets.Scripts.Utils;

public class LoginViewModel : ViewModelBase
{
    [SerializeField] private EventDefinition _loginEventChannel;

    private TextField _usernameField;
    private TextField _passwordField;
    private Button _loginButton;
    private Label _errorLabel;
    private Label _registerLabel;

    private void OnEnable()
    {
        SessionManager.LoadFromPrefs();
        var root = GetComponent<UIDocument>().rootVisualElement;
        _usernameField = root.Q<TextField>("username-field");
        _passwordField = root.Q<TextField>("password-field");
        _loginButton = root.Q<Button>("login-button");
        _errorLabel = root.Q<Label>("error-label");
        _registerLabel = root.Q<Label>("register-link");

        root.Q<Label>("title").text    = LocalisationKeys.AppTitle.Localize();
        root.Q<Label>("subtitle").text = LocalisationKeys.LoginSubtitle.Localize();
        _usernameField.textEdition.placeholder = LocalisationKeys.UsernamePlaceholder.Localize();
        _passwordField.textEdition.placeholder = LocalisationKeys.PasswordPlaceholder.Localize();
        _loginButton.text   = LocalisationKeys.LaunchButton.Localize();
        _registerLabel.text = LocalisationKeys.RegisterLink.Localize();

        _loginButton.clicked += OnLoginClicked;

        _registerLabel.RegisterCallback<ClickEvent>(_ => NavigationManager.Instance.Replace("RegisterView"));
        StartCoroutine(ReconnectCoroutine());
    }

    private void OnDisable()
    {
        _loginButton.clicked -= OnLoginClicked;
    }

    private void OnLoginClicked()
    {
        _errorLabel.style.display = DisplayStyle.None;
        StartCoroutine(LoginCoroutine(_usernameField.value, _passwordField.value));
    }

    private IEnumerator ReconnectCoroutine()
    {
        if (string.IsNullOrEmpty(SessionManager.Token))
            yield break;

        
        yield return ApiService.Call(
            ApiService.Api.ReconnectAsync,
            response =>
            {
                SessionManager.Token = response.Token;
                SessionManager.UserId = response.UserId;
                SessionManager.RoleId = response.RoleId;
                SessionManager.FactionId = response.FactionId;
                SessionManager.LocationPlanetId = response.LocationPlanetId;
                SessionManager.LatestGalaxyPull = null;
                Debug.Log($"Reconnection successful! UserId: {response.UserId}");
                MatchmakingHubService.ConnectAsync();
                _loginEventChannel.Raise(new());
                NavigationManager.Instance.SetPersistent("HeaderView");
                NavigationManager.Instance.SetPersistent("PartyView");
                NavigationManager.Instance.Replace("SelectedPlanetView");
            },
            error =>
            {
                Debug.LogError($"Reconnection failed [{error.StatusCode}]: {error.Message}");
            }
        );
    }

    private IEnumerator LoginCoroutine(string username, string password)
    {
        yield return ApiService.Call(()=>ApiService.Api.LoginAsync(
            new() { 
                Username = username, 
                Password = password 
            }),
            response =>
            {
                SessionManager.Token = response.Token;
                SessionManager.UserId = response.UserId;
                SessionManager.RoleId = response.RoleId;
                SessionManager.FactionId = response.FactionId;
                SessionManager.LocationPlanetId = response.LocationPlanetId;
                SessionManager.LatestGalaxyPull = null;

                Debug.Log($"Login successful! UserId: {response.UserId}");
                MatchmakingHubService.ConnectAsync();
                _loginEventChannel.Raise(new());

                NavigationManager.Instance.SetPersistent("HeaderView");
                NavigationManager.Instance.SetPersistent("PartyView");
                NavigationManager.Instance.Replace("SelectedPlanetView");
            },
            error =>
            {
                ShowError(error.Message);
                Debug.LogError($"Login failed [{error.StatusCode}]: {error.Message}");
            }
        );
    }

    private void ShowError(string message)
    {
        _errorLabel.text = message;
        _errorLabel.style.display = DisplayStyle.Flex;
    }
}
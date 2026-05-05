using Assets.Scripts.Utils;
using Assets.Scripts.Utils.I18N;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.MVVM.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private TextField _usernameField;
        private TextField _emailField;
        private TextField _passwordField;
        private Button _registerButton;
        private Label _errorLabel;
        private Label _loginLink;

        private VisualElement _mandateFaction;
        private VisualElement _coalitionFaction;
        private VisualElement _cartelFaction;

        private int _selectedFactionId = -1;

        private const int CoalitionId = 1;
        private const int CartelId = 2;
        private const int MandateId = 3;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _usernameField = root.Q<TextField>("username-field");
            _emailField = root.Q<TextField>("email-field");
            _passwordField = root.Q<TextField>("password-field");
            _registerButton = root.Q<Button>("register-button");
            _loginLink = root.Q<Label>("login-link");
            _errorLabel = root.Q<Label>("error-label");

            _mandateFaction = root.Q<VisualElement>("faction-mandate");
            _coalitionFaction = root.Q<VisualElement>("faction-coalition");
            _cartelFaction = root.Q<VisualElement>("faction-cartel");

            root.Q<Label>("title").text    = LocalisationKeys.AppTitle.Localize();
            root.Q<Label>("subtitle").text = LocalisationKeys.RegisterSubtitle.Localize();
            _usernameField.textEdition.placeholder = LocalisationKeys.UsernamePlaceholder.Localize();
            _emailField.textEdition.placeholder    = LocalisationKeys.EmailPlaceholder.Localize();
            _passwordField.textEdition.placeholder = LocalisationKeys.PasswordPlaceholder.Localize();
            _registerButton.text = LocalisationKeys.LaunchButton.Localize();
            _loginLink.text      = LocalisationKeys.LoginLink.Localize();

            _mandateFaction.RegisterCallback<ClickEvent>(_ => SelectFaction(_mandateFaction, MandateId));
            _coalitionFaction.RegisterCallback<ClickEvent>(_ => SelectFaction(_coalitionFaction, CoalitionId));
            _cartelFaction.RegisterCallback<ClickEvent>(_ => SelectFaction(_cartelFaction, CartelId));

            _loginLink.RegisterCallback<ClickEvent>(_ => NavigationManager.Instance.Replace("LoginView"));
            _registerButton.clicked += OnRegisterClicked;
        }

        private void OnDisable()
        {
            _registerButton.clicked -= OnRegisterClicked;
        }

        private void SelectFaction(VisualElement selected, int factionId)
        {
            _selectedFactionId = factionId;

            foreach (var faction in new[] { _mandateFaction, _coalitionFaction, _cartelFaction })
            {
                faction.style.borderTopWidth = 0;
                faction.style.borderBottomWidth = 0;
                faction.style.borderLeftWidth = 0;
                faction.style.borderRightWidth = 0;
                faction.style.opacity = 0.5f;
            }

            selected.style.borderTopColor = new StyleColor(new Color(0.49f, 0.78f, 0.89f));
            selected.style.borderBottomColor = new StyleColor(new Color(0.49f, 0.78f, 0.89f));
            selected.style.borderLeftColor = new StyleColor(new Color(0.49f, 0.78f, 0.89f));
            selected.style.borderRightColor = new StyleColor(new Color(0.49f, 0.78f, 0.89f));
            selected.style.borderTopWidth = 2;
            selected.style.borderBottomWidth = 2;
            selected.style.borderLeftWidth = 2;
            selected.style.borderRightWidth = 2;
            selected.style.opacity = 1f;
        }

        private void OnRegisterClicked()
        {
            _errorLabel.style.display = DisplayStyle.None;

            if (!ValidateFields()) return;

            StartCoroutine(RegisterCoroutine(
                _usernameField.value.Trim(),
                _emailField.value.Trim(),
                _passwordField.value,
                _selectedFactionId
            ));
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(_usernameField.value))
            {
                ShowError("Username cannot be empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_emailField.value) || !_emailField.value.Contains("@"))
            {
                ShowError("Please enter a valid email.");
                return false;
            }

            if (_passwordField.value.Length < 6)
            {
                ShowError("Password must be at least 6 characters.");
                return false;
            }

            if (_selectedFactionId == -1)
            {
                ShowError("Please select a faction.");
                return false;
            }

            return true;
        }

        private IEnumerator RegisterCoroutine(string username, string email, string password, int factionId)
        {
            yield return ApiService.Call(
                ()=>ApiService.Api.RegisterAsync(new() { 
                    Email=email,
                    FactionId=factionId,
                    Password=password,
                    Username=username}),
                response =>
                {
                    SessionManager.Token = response.Token;
                    SessionManager.UserId = response.UserId;
                    SessionManager.RoleId = response.RoleId;
                    SessionManager.FactionId = response.FactionId;
                    SessionManager.LocationPlanetId = response.LocationPlanetId;
                    SessionManager.LatestGalaxyPull = null;
                    Debug.Log($"Register successful! UserId: {response.UserId}, FactionId: {factionId}");
                    NavigationManager.Instance.SetPersistent("HeaderView");
                    NavigationManager.Instance.Replace("SelectedPlanetView");
                },
                error =>
                {
                    ShowError(error.Message);
                    Debug.LogError($"Register failed [{error.StatusCode}]: {error.Message}");
                }
            );
        }

        private void ShowError(string message)
        {
            _errorLabel.text = message;
            _errorLabel.style.display = DisplayStyle.Flex;
        }
    }
}
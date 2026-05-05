using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NavigationManager : MonoBehaviour
{
    public static NavigationManager Instance { get; private set; }

    public GameObject Current => _stack.Count > 0 ? _stack.Peek() : null;

    [SerializeField] private string _initialView = "LoginView";

    [SerializeField] private List<GameObject> _viewPrefabs = new();

    private readonly Stack<GameObject> _stack = new();
    private readonly Dictionary<string, GameObject> _prefabMap = new();
    private readonly List<GameObject> _persistentViews = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPrefabMap();
        SessionManager.OnExpired += ReturnToInitial;
    }

    private void OnDestroy()
    {
        SessionManager.OnExpired -= ReturnToInitial;
    }

    private void Start()
    {
        Push(_initialView);
    }

    private void BuildPrefabMap()
    {
        _prefabMap.Clear();
        foreach (var prefab in _viewPrefabs)
        {
            if (prefab == null) continue;
            _prefabMap[prefab.name] = prefab;
            Debug.Log($"[NavigationManager] Registered view: {prefab.name}");
        }
    }

    public void Push(string viewName)
    {
        if (!ResolvePrefab(viewName, out var prefab)) return;
        Debug.Log($"[NavigationManager] Pushing view: {viewName}");

        if (_stack.Count > 0)
            HideView(_stack.Peek());

        _stack.Push(Instantiate(prefab));
    }

    public void Pop()
    {
        if (_stack.Count <= 1) return;
        Debug.Log($"[NavigationManager] Popping view: {_stack.Peek().name}");

        Destroy(_stack.Pop());

        if (_stack.Count > 0)
            ShowView(_stack.Peek());
    }

    private static void HideView(GameObject view)
    {
        var uiDoc = view.GetComponent<UIDocument>();
        if (uiDoc != null)
            uiDoc.rootVisualElement.style.display = DisplayStyle.None;
        else
            view.SetActive(false);
    }

    private static void ShowView(GameObject view)
    {
        var uiDoc = view.GetComponent<UIDocument>();
        if (uiDoc != null)
            uiDoc.rootVisualElement.style.display = DisplayStyle.Flex;
        else
            view.SetActive(true);
    }

    public void Replace(string viewName)
    {
        if (!ResolvePrefab(viewName, out var prefab)) return;
        Debug.Log($"[NavigationManager] Replacing current view with: {viewName}");

        if (_stack.Count > 0)
            Destroy(_stack.Pop());

        _stack.Push(Instantiate(prefab));
    }

    public void PopToRoot()
    {
        while (_stack.Count > 1)
            Destroy(_stack.Pop());

        if (_stack.Count > 0)
            _stack.Peek().SetActive(true);
    }

    public void SetPersistent(string viewName)
    {
        if (!ResolvePrefab(viewName, out var prefab)) return;
        Debug.Log($"[NavigationManager] Setting persistent view: {viewName}");
        _persistentViews.Add(Instantiate(prefab));
    }

    public void ClearPersistentViews()
    {
        foreach (var view in _persistentViews)
            if (view != null) Destroy(view);
        _persistentViews.Clear();
    }

    public void ReturnToInitial()
    {
        ClearPersistentViews();
        while (_stack.Count > 0)
            Destroy(_stack.Pop());

        Replace(_initialView);
    }

    private bool ResolvePrefab(string viewName, out GameObject prefab)
    {
        if (_prefabMap.TryGetValue(viewName, out prefab)) return true;

        Debug.LogError($"[NavigationManager] No view prefab found with name '{viewName}'. " +
                       $"Available views: {string.Join(", ", _prefabMap.Keys)}");
        return false;
    }

#if UNITY_EDITOR
    [ContextMenu("Reload View Prefabs")]
    private void ReloadViewPrefabs()
    {
        _viewPrefabs.Clear();
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Views" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.name.EndsWith("View"))
            {
                _viewPrefabs.Add(prefab);
                Debug.Log($"[NavigationManager] Found: {prefab.name} at {path}");
            }
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[NavigationManager] Loaded {_viewPrefabs.Count} view prefabs.");
    }
#endif
}
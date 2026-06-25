using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Mounted in VNMainMenu. Cleans stale persistent VN UI before rebuilding the
/// main menu, so a story restarted after Pacman does not inherit old input refs.
/// </summary>
public class MainMenuReset : MonoBehaviour
{
    private void Awake()
    {
        CleanupDontDestroyOnLoadUI();
        ResetUIManagerCache();

        FixCanvasAndPanel();
        RestoreMainMenuEventSystem();

        try { ResetGameState(); }
        catch (System.Exception e) { Debug.LogError($"[MainMenuReset] GameState reset failed: {e.Message}"); }

        try { ResetVNManager(); }
        catch (System.Exception e) { Debug.LogError($"[MainMenuReset] VNManager reset failed: {e.Message}"); }

        if (Time.timeScale != 1f) Time.timeScale = 1f;

        Destroy(this);
    }

    private void FixCanvasAndPanel()
    {
        var ui = UIManager.GetInstance();
        ui.Init();

        var allPanels = FindObjectsByType<MainMenuPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in allPanels)
        {
            if (p != null && p.gameObject != null)
                Destroy(p.gameObject);
        }

        if (ui.panelDic.ContainsKey("MainMenuPanel"))
            ui.panelDic.Remove("MainMenuPanel");

        var allPause = FindObjectsByType<PausePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in allPause)
        {
            if (p != null && p.gameObject != null && p.gameObject.scene.name == "DontDestroyOnLoad")
                Destroy(p.gameObject);
        }

        string path = VNProjectConfig.Instance != null && !string.IsNullOrEmpty(VNProjectConfig.Instance.UI_MainMenuPath)
            ? VNProjectConfig.Instance.UI_MainMenuPath
            : "VNPrefabs/UI/MainMenu";

        ui.ShowPanel<MainMenuPanel>("MainMenuPanel", path, E_UI_Layer.Middle, null);
        Canvas.ForceUpdateCanvases();
    }

    private void CleanupDontDestroyOnLoadUI()
    {
        GameObject temp = new GameObject("TempProbe");
        DontDestroyOnLoad(temp);
        Scene dontDestroyScene = temp.scene;
        Destroy(temp);

        int cleanedCount = 0;

        foreach (GameObject rootObj in dontDestroyScene.GetRootGameObjects())
        {
            if (rootObj == null) continue;

            if (rootObj.name.Contains("MonoManager"))
                continue;

            bool hasPersistentLoading = rootObj.GetComponentInChildren<LoadingProgressPanel>(true) != null;
            if (hasPersistentLoading)
                continue;

            if (rootObj.GetComponent<SceneLoader>() != null)
            {
                DestroyPersistentRoot(rootObj, "stale SceneLoader");
                cleanedCount++;
                continue;
            }

            if (rootObj.GetComponent<PacScripts.Jump2Pac>() != null)
            {
                continue;
            }

            if (rootObj.GetComponentInChildren<VNGameplayPanel>(true) != null)
            {
                DestroyPersistentRoot(rootObj, "stale VN gameplay panel");
                cleanedCount++;
                continue;
            }

            if (rootObj.GetComponent<Canvas>() != null)
            {
                DestroyPersistentRoot(rootObj, "stale Canvas");
                cleanedCount++;
                continue;
            }

            if (rootObj.GetComponent<EventSystem>() != null)
            {
                DestroyPersistentRoot(rootObj, "stale EventSystem");
                cleanedCount++;
                continue;
            }

            bool hasInputModule = rootObj.GetComponent<StandaloneInputModule>() != null
                               || rootObj.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null;
            if (hasInputModule)
            {
                DestroyPersistentRoot(rootObj, "stale input module");
                cleanedCount++;
            }
        }

        Debug.Log($"[MainMenuReset] DontDestroyOnLoad cleanup complete: {cleanedCount}");
    }

    private void DestroyPersistentRoot(GameObject rootObj, string reason)
    {
        Debug.Log($"[MainMenuReset] Destroy {reason}: {rootObj.name}");
        rootObj.SetActive(false);
        Destroy(rootObj);
    }

    private void ResetUIManagerCache()
    {
        var ui = UIManager.GetInstance();
        if (ui == null) return;

        ui.canvas = null;
        ui.panelDic.Clear();
        SetUIManagerPrivateField(ui, "_canvasGameObject", null);
        SetUIManagerPrivateField(ui, "_eventSystemGameObject", null);
        SetUIManagerPrivateField(ui, "_isCanvasDynamicallyCreated", false);
        SetUIManagerPrivateField(ui, "_isEventSystemDynamicallyCreated", false);

        Debug.Log("[MainMenuReset] UIManager canvas/event system cache cleared");
    }

    private void SetUIManagerPrivateField(UIManager ui, string fieldName, object value)
    {
        FieldInfo field = typeof(UIManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(ui, value);
    }

    private void RestoreMainMenuEventSystem()
    {
        EventSystem selected = null;
        Scene activeScene = SceneManager.GetActiveScene();

        foreach (EventSystem es in FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (es == null) continue;

            bool isMainMenuEventSystem = es.gameObject.scene == activeScene && es.gameObject.scene.name != "DontDestroyOnLoad";
            es.gameObject.SetActive(isMainMenuEventSystem);
            es.enabled = isMainMenuEventSystem;

            foreach (BaseInputModule module in es.GetComponents<BaseInputModule>())
                module.enabled = isMainMenuEventSystem;

            if (isMainMenuEventSystem && selected == null)
                selected = es;
        }

        if (selected != null)
        {
            EventSystem.current = selected;
            selected.SetSelectedGameObject(null);
            SetUIManagerPrivateField(UIManager.GetInstance(), "_eventSystemGameObject", selected.gameObject);
            SetUIManagerPrivateField(UIManager.GetInstance(), "_isEventSystemDynamicallyCreated", false);
            Debug.Log($"[MainMenuReset] Main menu EventSystem selected: {selected.name}");
        }
        else
        {
            Debug.LogWarning("[MainMenuReset] Main menu EventSystem not found");
        }
    }

    private void ResetGameState()
    {
        var gsm = GameStateManager.GetInstance();
        if (gsm == null)
        {
            Debug.LogWarning("[MainMenuReset] GameStateManager missing, skip reset");
            return;
        }

        if (gsm.IsStateStackEmpty() && gsm.CurrentState == GameState.Gameplay)
        {
            Debug.Log("[MainMenuReset] GameStateManager already clean, skip reset");
            return;
        }

        gsm.ResetToMainMenu();
        Debug.Log("[MainMenuReset] GameStateManager reset complete");
    }

    private void ResetVNManager()
    {
        var vnManager = VNManager.GetInstance();
        if (vnManager == null)
        {
            Debug.LogWarning("[MainMenuReset] VNManager missing, skip reset");
            return;
        }

        if (vnManager.StoryLines.Count == 0 && vnManager.CurrentLineIndex < 0)
        {
            Debug.Log("[MainMenuReset] VNManager already clean, skip reset");
            return;
        }

        vnManager.ResetForMainMenu();
        Debug.Log("[MainMenuReset] VNManager reset complete");
    }
}

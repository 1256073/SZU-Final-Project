using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    public GameObject mainCanvas;
    public GameObject mainEventSystem;

    private string currentMiniGameScene;
    public bool IsMiniGameRunning { get; private set; }

    /// <summary>VN 场景路径（启动时缓存，用于重新加载）</summary>
    private string vnScenePath;
    /// <summary>VN 场景中所有 Canvas（用于批量禁用/恢复）</summary>
    private List<Canvas> vnCanvases = new List<Canvas>();
    /// <summary>VN 场景中所有 EventSystem（用于批量禁用/恢复）</summary>
    private List<EventSystem> vnEventSystems = new List<EventSystem>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // 必须在 DontDestroyOnLoad 之前记录 VN 场景路径
        vnScenePath = gameObject.scene.path;
        Debug.Log($"[SceneLoader] Awake - VN 场景路径: {vnScenePath}");

        DontDestroyOnLoad(gameObject);

        CacheVNUIReferences();
    }

    /// <summary>缓存当前 VN 场景中的所有 Canvas 和 EventSystem 引用</summary>
    private void CacheVNUIReferences()
    {
        vnCanvases.Clear();
        vnEventSystems.Clear();

        Scene vnScene = GetVNScene();
        if (!vnScene.isLoaded) return;

        foreach (var root in vnScene.GetRootGameObjects())
        {
            var canvases = root.GetComponentsInChildren<Canvas>(includeInactive: true);
            vnCanvases.AddRange(canvases);

            var eventSystems = root.GetComponentsInChildren<EventSystem>(includeInactive: true);
            vnEventSystems.AddRange(eventSystems);
        }
    }

    /// <summary>按路径查找 VN 场景</summary>
    private Scene GetVNScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.path == vnScenePath) return s;
        }
        return default;
    }

    public void LoadMiniGame(string sceneName)
    {
        if (IsMiniGameRunning) return;

        currentMiniGameScene = sceneName;
        IsMiniGameRunning = true;

        // 强制禁用所有 VN 侧 UI
        DisableVNUI();

        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;
    }

    public void UnloadMiniGame()
    {
        if (!IsMiniGameRunning) return;
        StartCoroutine(UnloadRoutine());
    }

    IEnumerator UnloadRoutine()
    {
        // 1. 如果 VN 场景丢失，重新加载
        Scene vnScene = GetVNScene();
        if (!vnScene.isLoaded)
        {
            Debug.Log($"[SceneLoader] VN 场景已丢失，重新加载: {vnScenePath}");
            AsyncOperation reloadOp = SceneManager.LoadSceneAsync(vnScenePath, LoadSceneMode.Additive);
            while (!reloadOp.isDone) yield return null;
            // 重新缓存 UI 引用（旧引用已失效）
            CacheVNUIReferences();
        }

        // 2. 卸载所有不属于 VN 场景的非持久场景
        int sceneCount = SceneManager.sceneCount;
        for (int i = sceneCount - 1; i >= 0; i--)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.path != vnScenePath && s.name != "DontDestroyOnLoad")
            {
                Debug.Log($"[SceneLoader] 卸载小游戏场景: {s.name}");
                AsyncOperation op = SceneManager.UnloadSceneAsync(s);
                if (op != null)
                {
                    while (!op.isDone) yield return null;
                }
                else
                {
                    Debug.LogWarning($"[SceneLoader] 无法卸载场景: {s.name}");
                }
            }
        }

        currentMiniGameScene = null;
        IsMiniGameRunning = false;

        // 3. 恢复 VN UI
        EnableVNUI();
    }

    // ==================== VN UI 批量控制 ====================

    private void DisableVNUI()
    {
        if (mainCanvas) mainCanvas.SetActive(false);
        if (mainEventSystem) mainEventSystem.SetActive(false);

        foreach (var c in vnCanvases)
        {
            if (c != null && c.gameObject != mainCanvas)
                c.gameObject.SetActive(false);
        }

        foreach (var es in vnEventSystems)
        {
            if (es != null && es.gameObject != mainEventSystem)
                es.gameObject.SetActive(false);
        }
    }

    private void EnableVNUI()
    {
        // 先禁掉非 VN 场景的 EventSystem，防止冲突
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.path != vnScenePath && s.name != "DontDestroyOnLoad")
            {
                foreach (var root in s.GetRootGameObjects())
                {
                    foreach (var es in root.GetComponentsInChildren<EventSystem>())
                    {
                        if (es != null) es.gameObject.SetActive(false);
                    }
                }
            }
        }

        // 恢复 VN UI
        if (mainCanvas) mainCanvas.SetActive(true);
        if (mainEventSystem) mainEventSystem.SetActive(true);

        foreach (var c in vnCanvases)
        {
            if (c != null && c.gameObject != mainCanvas)
                c.gameObject.SetActive(true);
        }

        foreach (var es in vnEventSystems)
        {
            if (es != null && es.gameObject != mainEventSystem)
                es.gameObject.SetActive(true);
        }
    }
}
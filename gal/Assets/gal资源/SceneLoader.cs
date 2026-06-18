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
    /// <summary>VN 场景中所有 AudioListener（进入小游戏时禁用，防止多 AudioListener）</summary>
    private List<AudioListener> vnAudioListeners = new List<AudioListener>();
    /// <summary>SceneLoader 自带的兜底 AudioListener（DontDestroyOnLoad，VN 场景丢失时保证始终有一个）</summary>
    private AudioListener fallbackAudioListener;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // 必须在 DontDestroyOnLoad 之前记录 VN 场景路径
        vnScenePath = gameObject.scene.path;
        Debug.Log($"[SceneLoader] Awake - VN 场景路径: {vnScenePath}");

        DontDestroyOnLoad(gameObject);

        // 创建兜底 AudioListener：SceneLoader 是 DontDestroyOnLoad，此 Listener 永不销毁
        fallbackAudioListener = GetComponent<AudioListener>();
        if (fallbackAudioListener == null)
            fallbackAudioListener = gameObject.AddComponent<AudioListener>();
        fallbackAudioListener.enabled = true;

        CacheVNUIReferences();

        // 禁用 VN 场景自带的 AudioListener，统一由兜底 Listener 接管
        foreach (var al in vnAudioListeners)
        {
            if (al != null)
                al.enabled = false;
        }
    }

    /// <summary>缓存当前 VN 场景中的所有 Canvas 和 EventSystem 引用</summary>
    private void CacheVNUIReferences()
    {
        vnCanvases.Clear();
        vnEventSystems.Clear();
        vnAudioListeners.Clear();

        Scene vnScene = GetVNScene();
        if (!vnScene.isLoaded) return;

        foreach (var root in vnScene.GetRootGameObjects())
        {
            var canvases = root.GetComponentsInChildren<Canvas>(includeInactive: true);
            vnCanvases.AddRange(canvases);

            var eventSystems = root.GetComponentsInChildren<EventSystem>(includeInactive: true);
            vnEventSystems.AddRange(eventSystems);

            var audioListeners = root.GetComponentsInChildren<AudioListener>(includeInactive: true);
            vnAudioListeners.AddRange(audioListeners);
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
        // 卸载所有非持久、非 VN 的场景
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
                    // 场景无法卸载（VN 场景已丢导致它是唯一场景）
                    // 退而求其次：禁用该场景所有根对象
                    Debug.LogWarning($"[SceneLoader] 无法卸载场景: {s.name}，改为禁用所有根对象");
                    foreach (var root in s.GetRootGameObjects())
                        root.SetActive(false);
                }
            }
        }

        currentMiniGameScene = null;
        IsMiniGameRunning = false;

        // 清理小游戏残留的 DontDestroyOnLoad BGM 播放器（兜底）
        var bgmPlayer = GameObject.Find("BGM_Player");
        if (bgmPlayer != null)
            Destroy(bgmPlayer);

        // 恢复 VN UI（如果 VN 场景还活着的话）
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

        // 禁用 VN 场景的 AudioListener（如果还存在）
        foreach (var al in vnAudioListeners)
        {
            if (al != null)
                al.enabled = false;
        }

        // 禁用兜底 AudioListener，让小游戏独占
        if (fallbackAudioListener != null)
            fallbackAudioListener.enabled = false;
    }

    private void EnableVNUI()
    {
        // 1. 禁用所有非 VN 场景中的 Canvas、EventSystem 和 AudioListener（防止冲突）
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.path != vnScenePath && s.name != "DontDestroyOnLoad")
            {
                foreach (var root in s.GetRootGameObjects())
                {
                    // 禁用 Canvas 防止覆盖 VN UI
                    foreach (var canvas in root.GetComponentsInChildren<Canvas>())
                        if (canvas != null) canvas.gameObject.SetActive(false);
                    // 禁用 EventSystem 防止输入冲突
                    foreach (var es in root.GetComponentsInChildren<EventSystem>())
                        if (es != null) es.gameObject.SetActive(false);
                    // 禁用 AudioListener 防止出现多个 AudioListener
                    foreach (var al in root.GetComponentsInChildren<AudioListener>())
                        if (al != null) al.enabled = false;
                }
            }
        }

        // 2. 禁用 VN 场景中可能残留的 AudioListener（避免与兜底 Listener 冲突）
        foreach (var al in vnAudioListeners)
        {
            if (al != null)
                al.enabled = false;
        }

        // 3. 隐藏 UIManager 在 Additive 场景加载时误创建的 MainMenuPanel
        var mainMenu = Object.FindFirstObjectByType<MainMenuPanel>(FindObjectsInactive.Include);
        if (mainMenu != null) mainMenu.gameObject.SetActive(false);

        // 4. 恢复 VN UI
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

        // 5. 启用兜底 AudioListener（DontDestroyOnLoad，保证始终有一个）
        if (fallbackAudioListener != null)
            fallbackAudioListener.enabled = true;
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    /// <summary>VN 场景中所有 AudioSource（进入小游戏时暂停，防止剧情 BGM 继续播放）</summary>
    private List<AudioSource> vnAudioSources = new List<AudioSource>();
    /// <summary>SceneLoader 自带的兜底 AudioListener（DontDestroyOnLoad，VN 场景丢失时保证始终有一个）</summary>
    private AudioListener fallbackAudioListener;

    /// <summary>VN 场景中所有 Camera（用于批量禁用/恢复，防止小游戏返回后 VN 摄像机未恢复）</summary>
    private List<Camera> vnCameras = new List<Camera>();
    private readonly Dictionary<GameObject, bool> savedGameObjectActiveStates = new Dictionary<GameObject, bool>();
    private readonly Dictionary<Behaviour, bool> savedBehaviourEnabledStates = new Dictionary<Behaviour, bool>();
    private Scene previousActiveScene;
    private bool hasSavedVNUIState;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Instance.IsManagedVNSceneLoaded())
            {
                Destroy(gameObject);
                return;
            }

            Debug.LogWarning("[SceneLoader] Replacing stale SceneLoader instance");
            Destroy(Instance.gameObject);
        }

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

        RestoreVNEventSystem();
        BindUIManagerToVNCanvas();
        BindUIManagerToVNEventSystem();
    }

    /// <summary>缓存当前 VN 场景中的所有 Canvas、EventSystem、AudioListener、Camera 引用</summary>
    private bool IsManagedVNSceneLoaded()
    {
        if (string.IsNullOrEmpty(vnScenePath))
            return false;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.path == vnScenePath)
                return true;
        }

        return false;
    }

    private void CacheVNUIReferences()
    {
        vnCanvases.Clear();
        vnEventSystems.Clear();
        vnAudioListeners.Clear();
        vnAudioSources.Clear();
        vnCameras.Clear();

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

            var audioSources = root.GetComponentsInChildren<AudioSource>(includeInactive: true);
            vnAudioSources.AddRange(audioSources);

            var cameras = root.GetComponentsInChildren<Camera>(includeInactive: true);
            vnCameras.AddRange(cameras);
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
        CaptureVNUIState();

        // 强制禁用所有 VN 侧 UI
        DisableVNUI();

        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        Scene miniGameScene = SceneManager.GetSceneByName(sceneName);
        if (miniGameScene.IsValid() && miniGameScene.isLoaded)
        {
            SceneManager.SetActiveScene(miniGameScene);
        }

        yield return null;
        DisableVNUI();
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
        PacScripts.IniPac.StopBGM();
        CleanupMiniGamePersistentObjects();

        // 【Bug修复】如果 VN 场景在小游戏过程中被意外卸载，重新加载
        bool vnSceneLost = !GetVNScene().isLoaded;

        if (vnSceneLost && !string.IsNullOrEmpty(vnScenePath))
        {
            Debug.LogWarning($"[SceneLoader] VN 场景已丢失，正在重新加载: {vnScenePath}");

            // 【关键】清除 VNManager 的跨场景待处理状态，
            // 防止 OnSceneLoaded 检测到残留的 pendingScriptName 而重启游戏
            VNManager.GetInstance().ClearPendingSceneState();

            AsyncOperation reloadOp = SceneManager.LoadSceneAsync(vnScenePath, LoadSceneMode.Additive);
            while (!reloadOp.isDone) yield return null;
            Debug.Log("[SceneLoader] VN 场景已重新加载");

            // 等待一帧，让 UIManager.OnSceneLoaded 等回调先执行
            yield return null;

            // 刷新缓存：重新扫描 VN 场景中的 Canvas/EventSystem/Camera
            CacheVNUIReferences();
            // 再等一帧，确保 UIManager.DelayedInitGameplayUI 也执行完
            yield return null;
        }

        // 恢复 VN UI（如果 VN 场景还活着的话）
        EnableVNUI();
    }

    // ==================== VN UI 批量控制 ====================

    private void CaptureVNUIState()
    {
        CacheVNUIReferences();

        savedGameObjectActiveStates.Clear();
        savedBehaviourEnabledStates.Clear();
        previousActiveScene = SceneManager.GetActiveScene();
        hasSavedVNUIState = true;

        SaveGameObjectState(mainCanvas);
        SaveGameObjectState(mainEventSystem);

        foreach (var c in vnCanvases)
        {
            if (c == null) continue;
            SaveGameObjectState(c.gameObject);
            SaveBehaviourState(c);
        }

        foreach (var es in vnEventSystems)
        {
            if (es == null) continue;
            SaveGameObjectState(es.gameObject);
            SaveBehaviourState(es);
            foreach (var module in es.GetComponents<BaseInputModule>())
                SaveBehaviourState(module);
        }

        foreach (var al in vnAudioListeners)
        {
            if (al == null) continue;
            SaveBehaviourState(al);
        }
    }

    private void SaveGameObjectState(GameObject go)
    {
        if (go != null && !savedGameObjectActiveStates.ContainsKey(go))
            savedGameObjectActiveStates.Add(go, go.activeSelf);
    }

    private void SaveBehaviourState(Behaviour behaviour)
    {
        if (behaviour != null && !savedBehaviourEnabledStates.ContainsKey(behaviour))
            savedBehaviourEnabledStates.Add(behaviour, behaviour.enabled);
    }

    private void RestoreVNUIState()
    {
        if (!hasSavedVNUIState)
            return;

        foreach (var pair in savedGameObjectActiveStates)
        {
            if (pair.Key != null)
                pair.Key.SetActive(pair.Value);
        }

        foreach (var pair in savedBehaviourEnabledStates)
        {
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;
        }

        savedGameObjectActiveStates.Clear();
        savedBehaviourEnabledStates.Clear();
        hasSavedVNUIState = false;
    }

    private void DisableVNUI()
    {
        if (mainCanvas)
        {
            mainCanvas.SetActive(false);
        }
        if (mainEventSystem)
        {
            mainEventSystem.SetActive(false);
        }

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

        // 注意：不主动禁用 VN Camera.enabled
        // 小游戏的 Camera depth 更高会自动覆盖渲染，禁用 Camera 组件会在 URP 中丢失 Base Camera 注册

        // 禁用 VN 场景的 AudioListener（如果还存在）
        foreach (var al in vnAudioListeners)
        {
            if (al != null)
                al.enabled = false;
        }

        // 禁用兜底 AudioListener，让小游戏独占
        if (fallbackAudioListener != null)
            fallbackAudioListener.enabled = false;

        // 暂停 VN 场景所有 AudioSource（停止剧情 BGM）
        foreach (var src in vnAudioSources)
        {
            if (src != null && src.isPlaying)
                src.Pause();
        }
    }

    private void EnableVNUI()
    {
        // 1. 禁用所有非 VN 场景中的 Canvas、EventSystem、Camera 和 AudioListener（防止冲突）
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
                    // 禁用 Camera 防止抢占 VN 摄像机渲染
                    foreach (var cam in root.GetComponentsInChildren<Camera>())
                        if (cam != null) cam.enabled = false;
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

        // 4. 恢复 VN UI（Canvas 激活会级联激活其子节点的 Camera）
        RestoreVNUIState();
        RestoreVNEventSystem();
        RestoreVNGameplayVisibility();

        // 5. 确保 VN 场景的 Camera 处于渲染状态
        //    VN Camera 进入小游戏时未禁用 enabled（仅 Canvas 被 SetActive），
        //    但如果 Camera 不在 Canvas 子级下，其 enabled 应仍为 true
        RefreshVNCameraState();

        // 6. 恢复 VN 场景所有 AudioSource（恢复剧情 BGM）
        foreach (var src in vnAudioSources)
        {
            if (src != null && src.clip != null)
                src.UnPause();
        }

        // 7. 启用兜底 AudioListener（DontDestroyOnLoad，保证始终有一个）
        if (fallbackAudioListener != null)
            fallbackAudioListener.enabled = true;

        Scene vnScene = GetVNScene();
        if (vnScene.IsValid() && vnScene.isLoaded)
        {
            SceneManager.SetActiveScene(vnScene);
        }
        else if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(previousActiveScene);
        }

        BindUIManagerToVNCanvas();
        BindUIManagerToVNEventSystem();
        UIManager.GetInstance().Init();
        BindUIManagerToVNEventSystem();
        RestoreVNGameplayVisibility();
        ClearResidualTransitionOverlays();
    }

    private void RestoreVNEventSystem()
    {
        EventSystem selected = null;

        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (es == null)
                continue;

            if (!IsVNEventSystem(es))
            {
                if (es.gameObject.activeSelf)
                    es.gameObject.SetActive(false);
                continue;
            }

            if (!es.gameObject.activeSelf)
                es.gameObject.SetActive(true);
            es.enabled = true;

            foreach (var module in es.GetComponents<BaseInputModule>())
                module.enabled = true;

            if (es.gameObject.activeInHierarchy && selected == null)
                selected = es;
        }

        if (selected != null)
        {
            EventSystem.current = selected;
            selected.SetSelectedGameObject(null);
            Debug.Log($"[SceneLoader] VN EventSystem selected: {selected.name} ({selected.gameObject.scene.name})");
        }
    }

    private void RestoreVNGameplayVisibility()
    {
        CacheVNUIReferences();

        foreach (var canvas in vnCanvases)
        {
            if (canvas == null)
                continue;

            bool isGameplayCanvas = canvas.name == "VNGamePlayCanvas" || canvas.gameObject == mainCanvas;
            if (!isGameplayCanvas)
                continue;

            canvas.gameObject.SetActive(true);
            canvas.enabled = true;
            AssignCanvasCamera(canvas);
        }

        if (mainCanvas != null)
        {
            mainCanvas.SetActive(true);
            var canvas = mainCanvas.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = true;
                AssignCanvasCamera(canvas);
            }
        }

        foreach (var gameplayPanel in Object.FindObjectsByType<VNGameplayPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (gameplayPanel != null && gameplayPanel.gameObject.scene.path == vnScenePath)
                gameplayPanel.ShowMe();
        }

        Canvas.ForceUpdateCanvases();
    }

    private void AssignCanvasCamera(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceCamera)
            return;

        Camera vnCamera = null;
        foreach (var cam in vnCameras)
        {
            if (cam == null || cam.gameObject.scene.path != vnScenePath)
                continue;

            vnCamera = cam;
            break;
        }

        if (vnCamera == null)
            vnCamera = Camera.main;

        if (vnCamera != null)
            canvas.worldCamera = vnCamera;
    }

    private void BindUIManagerToVNCanvas()
    {
        Canvas targetCanvas = null;
        foreach (var canvas in vnCanvases)
        {
            if (canvas == null || canvas.name != "VNGamePlayCanvas")
                continue;

            targetCanvas = canvas;
            break;
        }

        if (targetCanvas == null && mainCanvas != null)
            targetCanvas = mainCanvas.GetComponent<Canvas>();

        if (targetCanvas == null)
            return;

        targetCanvas.gameObject.SetActive(true);
        targetCanvas.enabled = true;

        UIManager uiManager = UIManager.GetInstance();
        uiManager.canvas = targetCanvas.transform as RectTransform;

        SetUIManagerPrivateField("_canvasGameObject", targetCanvas.gameObject);
        SetUIManagerPrivateField("_isCanvasDynamicallyCreated", false);
        Debug.Log($"[SceneLoader] UIManager 已绑定到 VN Canvas: {targetCanvas.name} ({targetCanvas.gameObject.scene.name})");
    }

    private void BindUIManagerToVNEventSystem()
    {
        EventSystem targetEventSystem = null;

        foreach (var eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (eventSystem == null || !IsVNEventSystem(eventSystem))
                continue;

            targetEventSystem = eventSystem;
            break;
        }

        if (targetEventSystem == null)
            return;

        targetEventSystem.gameObject.SetActive(true);
        targetEventSystem.enabled = true;

        foreach (var module in targetEventSystem.GetComponents<BaseInputModule>())
            module.enabled = true;

        foreach (var eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (eventSystem == null || eventSystem == targetEventSystem)
                continue;

            if (eventSystem.gameObject.activeSelf)
                eventSystem.gameObject.SetActive(false);
        }

        EventSystem.current = targetEventSystem;
        targetEventSystem.SetSelectedGameObject(null);
        SetUIManagerPrivateField("_eventSystemGameObject", targetEventSystem.gameObject);
        SetUIManagerPrivateField("_isEventSystemDynamicallyCreated", false);

        Debug.Log($"[SceneLoader] UIManager bound to VN EventSystem: {targetEventSystem.name} ({targetEventSystem.gameObject.scene.name})");
    }

    private bool IsVNEventSystem(EventSystem eventSystem)
    {
        if (eventSystem == null)
            return false;

        if (eventSystem.gameObject.scene.path == vnScenePath)
            return true;

        RectTransform uiCanvasTransform = UIManager.GetInstance().canvas;
        if (uiCanvasTransform == null)
        {
            foreach (var canvas in vnCanvases)
            {
                if (canvas != null && canvas.name == "VNGamePlayCanvas")
                {
                    uiCanvasTransform = canvas.transform as RectTransform;
                    break;
                }
            }
        }

        if (uiCanvasTransform == null)
            return false;

        if (eventSystem.transform.IsChildOf(uiCanvasTransform))
            return true;

        return eventSystem.gameObject.scene == uiCanvasTransform.gameObject.scene &&
               (eventSystem.name == "EventSystem" || eventSystem.GetComponent<BaseInputModule>() != null);
    }

    private void SetUIManagerPrivateField(string fieldName, object value)
    {
        FieldInfo field = typeof(UIManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(UIManager.GetInstance(), value);
    }

    private void ClearResidualTransitionOverlays()
    {
        int clearedFadeImages = 0;
        foreach (var image in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (image == null || image.name != "__DarkFadeImage")
                continue;

            Color color = image.color;
            color.r = 0f;
            color.g = 0f;
            color.b = 0f;
            color.a = 0f;
            image.color = color;
            image.raycastTarget = false;

            var canvas = image.GetComponentInParent<Canvas>(includeInactive: true);
            if (canvas != null)
            {
                canvas.enabled = true;
                canvas.sortingOrder = -10000;
            }
            clearedFadeImages++;
        }

        int hiddenLoadingPanels = 0;
        foreach (var loadingPanel in Object.FindObjectsByType<LoadingProgressPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (loadingPanel != null)
            {
                loadingPanel.gameObject.SetActive(false);
                hiddenLoadingPanels++;
            }
        }

        Debug.Log($"[SceneLoader] 已清理残留覆盖层: DarkFade={clearedFadeImages}, Loading={hiddenLoadingPanels}");
    }

    private void CleanupMiniGamePersistentObjects()
    {
        if (PacScripts.Jump2Pac.Instance != null)
        {
            Destroy(PacScripts.Jump2Pac.Instance.gameObject);
            Debug.Log("[SceneLoader] 已清理剧情小游戏路径残留的 Jump2Pac");
        }

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas == null || canvas.gameObject.scene.name != "DontDestroyOnLoad")
                continue;

            if (canvas.name == "VNGamePlayCanvas" ||
                canvas.name == "__DarkFadeCanvas" ||
                canvas.GetComponentInChildren<LoadingProgressPanel>(includeInactive: true) != null)
                continue;

            canvas.gameObject.SetActive(false);
            Debug.Log($"[SceneLoader] 已隐藏持久化小游戏 Canvas: {canvas.name}");
        }

        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (camera == null || camera.gameObject.scene.name != "DontDestroyOnLoad")
                continue;

            if (camera.gameObject.scene.path == vnScenePath)
                continue;

            camera.enabled = false;
            Debug.Log($"[SceneLoader] 已禁用持久化小游戏 Camera: {camera.name}");
        }
    }

    /// <summary>
    /// 确保 VN 场景的摄像机处于可渲染状态
    /// </summary>
    private void RefreshVNCameraState()
    {
        var allCameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int restored = 0;
        foreach (var cam in allCameras)
        {
            if (cam == null) continue;

            bool isVN = (cam.gameObject.scene.path == vnScenePath);
            if (isVN)
            {
                // 如果 Camera 的 GameObject 被父级关闭连带禁用，逐级激活
                if (!cam.gameObject.activeInHierarchy)
                {
                    cam.gameObject.SetActive(true);
                }
                // 确保 Camera 组件本身启用
                if (!cam.enabled)
                {
                    cam.enabled = true;
                }
                restored++;
            }
        }
        Debug.Log($"[SceneLoader] EnableVNUI - VN 摄像机已就绪: {restored} 个 (共检测 {allCameras.Length} 个摄像机)");
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// MainMenuReset — 挂载在 VNMainMenu 场景中
/// 负责在返回主菜单时清除所有残留的 DontDestroyOnLoad UI 对象
/// 并重置 VNovelizer 框架（VNManager / GameStateManager）的状态
/// </summary>
public class MainMenuReset : MonoBehaviour
{
    private void Awake()
    {
        // 修复 Canvas 渲染问题
        FixCanvasAndPanel();

        // ==================== 重置 GameStateManager 状态（仅在有残留时） ====================
        try { ResetGameState(); }
        catch (System.Exception e) { Debug.LogError($"[MainMenuReset] GameState重置异常: {e.Message}"); }

        // ==================== 重置 VNManager 状态（仅在有残留时） ====================
        try { ResetVNManager(); }
        catch (System.Exception e) { Debug.LogError($"[MainMenuReset] VNManager重置异常: {e.Message}"); }

        if (Time.timeScale != 1f) Time.timeScale = 1f;

        Destroy(this);
    }

    /// <summary>
    /// 修复 Canvas：强制重新创建 MainMenuPanel 到 Middle 层
    /// </summary>
    private void FixCanvasAndPanel()
    {
        var ui = UIManager.GetInstance();
        ui.Init();

        // 销毁所有已有的 MainMenuPanel（场景中的和 DontDestroyOnLoad 中的）
        var allPanels = FindObjectsByType<MainMenuPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in allPanels)
        {
            if (p != null && p.gameObject != null)
                Destroy(p.gameObject);
        }

        // 清除 UIManager 面板缓存，确保重新加载预制体
        if (ui.panelDic.ContainsKey("MainMenuPanel"))
            ui.panelDic.Remove("MainMenuPanel");

        // 同时清理残留的 PausePanel
        var allPause = FindObjectsByType<PausePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in allPause)
        {
            if (p != null && p.gameObject != null && p.gameObject.scene.name == "DontDestroyOnLoad")
                Destroy(p.gameObject);
        }

        // 强制从预制体重新加载 MainMenuPanel
        string path = VNProjectConfig.Instance != null && !string.IsNullOrEmpty(VNProjectConfig.Instance.UI_MainMenuPath)
            ? VNProjectConfig.Instance.UI_MainMenuPath
            : "VNPrefabs/UI/MainMenu";
        ui.ShowPanel<MainMenuPanel>("MainMenuPanel", path, E_UI_Layer.Middle, null);
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// 销毁 DontDestroyOnLoad 场景中所有残留的 Canvas 和 EventSystem
    /// 但保留 LoadingProgressPanel（UIManager 管理的常驻加载界面）
    /// </summary>
    private void CleanupDontDestroyOnLoadUI()
    {
        // 获取 DontDestroyOnLoad 场景
        GameObject temp = new GameObject("TempProbe");
        DontDestroyOnLoad(temp);
        Scene dontDestroyScene = temp.scene;
        Destroy(temp);

        var rootObjects = dontDestroyScene.GetRootGameObjects();
        int cleanedCount = 0;

        foreach (GameObject rootObj in rootObjects)
        {
            if (rootObj == null) continue;

            // 保留 SceneLoader 自身（它管理场景切换，必须存活）
            if (rootObj.GetComponent<SceneLoader>() != null) continue;

            // 保留 Jump2Pac 配置单例（小游戏入口需要它）
            if (rootObj.GetComponent<PacScripts.Jump2Pac>() != null) continue;

            // 保留 MonoManager（框架核心，必须存活）
            if (rootObj.name.Contains("MonoManager") || rootObj.GetComponent<MonoManager>() != null) continue;

            // 检查是否包含常驻 LoadingProgressPanel
            bool hasPersistentLoading = false;
            var loadingPanels = rootObj.GetComponentsInChildren<LoadingProgressPanel>(true);
            if (loadingPanels != null && loadingPanels.Length > 0)
                hasPersistentLoading = true;

            // 销毁残留的 VNGameplayPanel / ChoicePanel / PausePanel 等游戏面板
            // 这些面板在主菜单场景中不应该存在
            var vnGameplayPanel = rootObj.GetComponentInChildren<VNGameplayPanel>(true);
            if (vnGameplayPanel != null && !hasPersistentLoading)
            {
                Debug.Log($"[MainMenuReset] 销毁残留游戏面板 (含VNGameplayPanel): {rootObj.name}");
                Destroy(rootObj);
                cleanedCount++;
                continue;
            }

            // 销毁所有残留的 Canvas（不含 LoadingProgressPanel 所在的 Canvas）
            Canvas canvas = rootObj.GetComponent<Canvas>();
            if (canvas != null && !hasPersistentLoading)
            {
                Debug.Log($"[MainMenuReset] 销毁残留 Canvas: {rootObj.name}");
                Destroy(rootObj);
                cleanedCount++;
                continue;
            }

            // 销毁所有残留的 EventSystem（UIManager 会重新管理，保留场景自带的）
            EventSystem es = rootObj.GetComponent<EventSystem>();
            if (es != null)
            {
                Debug.Log($"[MainMenuReset] 销毁残留 EventSystem: {rootObj.name}");
                Destroy(rootObj);
                cleanedCount++;
                continue;
            }

            // 销毁残留的 StandaloneInputModule / InputSystemUIInputModule
            bool hasInputModule = rootObj.GetComponent<StandaloneInputModule>() != null
                               || rootObj.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null;
            if (hasInputModule)
            {
                Debug.Log($"[MainMenuReset] 销毁残留 InputModule: {rootObj.name}");
                Destroy(rootObj);
                cleanedCount++;
                continue;
            }
        }

        Debug.Log($"[MainMenuReset] DontDestroyOnLoad 清理完成，共清理 {cleanedCount} 个残留对象");
    }

    /// <summary>
    /// 重置 GameStateManager 到主菜单初始状态
    /// 仅在 GameStateManager 存在残留状态时才执行（全新启动时跳过）
    /// </summary>
    private void ResetGameState()
    {
        var gsm = GameStateManager.GetInstance();
        if (gsm == null)
        {
            Debug.LogWarning("[MainMenuReset] GameStateManager 实例不存在，跳过重置");
            return;
        }

        // 仅在状态栈非空或当前状态非 Gameplay 时才重置（全新启动时栈空且为 Gameplay，无需重置）
        if (gsm.IsStateStackEmpty() && gsm.CurrentState == GameState.Gameplay)
        {
            Debug.Log("[MainMenuReset] GameStateManager 无残留状态，跳过重置（全新启动）");
            return;
        }

        gsm.ResetToMainMenu();
        Debug.Log("[MainMenuReset] GameStateManager 已重置");
    }

    /// <summary>
    /// 重置 VNManager 到主菜单初始状态
    /// 仅在 VNManager 存在残留状态时才执行（全新启动时跳过）
    /// </summary>
    private void ResetVNManager()
    {
        var vnManager = VNManager.GetInstance();
        if (vnManager == null)
        {
            Debug.LogWarning("[MainMenuReset] VNManager 实例不存在，跳过重置");
            return;
        }

        // 仅在 VNManager 有残留数据时才重置（全新启动时 StoryLines 为空，无需重置）
        if (vnManager.StoryLines.Count == 0 && vnManager.CurrentLineIndex < 0)
        {
            Debug.Log("[MainMenuReset] VNManager 无残留状态，跳过重置（全新启动）");
            return;
        }

        vnManager.ResetForMainMenu();
        Debug.Log("[MainMenuReset] VNManager 已重置");
    }
}

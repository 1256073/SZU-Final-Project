using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    public GameObject mainCanvas;
    public GameObject mainEventSystem;

    private string currentMiniGameScene;
    public bool IsMiniGameRunning { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadMiniGame(string sceneName)
    {
        if (IsMiniGameRunning) return;
        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        // 隐藏主 UI，但不销毁
        if (mainCanvas) mainCanvas.SetActive(false);
        if (mainEventSystem) mainEventSystem.SetActive(false);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        currentMiniGameScene = sceneName;
        IsMiniGameRunning = true;
    }

    public void UnloadMiniGame()
    {
        if (!IsMiniGameRunning) return;
        StartCoroutine(UnloadRoutine());
    }

    IEnumerator UnloadRoutine()
    {
        if (string.IsNullOrEmpty(currentMiniGameScene)) yield break;
        AsyncOperation op = SceneManager.UnloadSceneAsync(currentMiniGameScene);
        while (!op.isDone) yield return null;

        currentMiniGameScene = null;
        IsMiniGameRunning = false;

        // 恢复主 UI
        if (mainCanvas) mainCanvas.SetActive(true);
        if (mainEventSystem) mainEventSystem.SetActive(true);
    }
}
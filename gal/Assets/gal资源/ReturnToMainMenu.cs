using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMainMenu : MonoBehaviour
{
    // 在 Inspector 中指定目标场景名称，默认 "VNMainMenu"
    public string targetSceneName = "VNMainMenu";

    public void GoToMainMenu()
    {
        // 重置框架状态，防止状态污染
        ResetFrameworkState();

        // 隐藏所有 DontDestroyOnLoad 中的 Canvas，避免残留 UI
        HidePersistentUI();

        SceneManager.LoadScene(targetSceneName);
    }

    /// <summary>
    /// 重置 VNovelizer 框架状态
    /// </summary>
    private void ResetFrameworkState()
    {
        // 重置 GameStateManager
        var gsm = GameStateManager.GetInstance();
        if (gsm != null)
        {
            gsm.ResetToMainMenu();
        }

        // 重置 VNManager
        var vnManager = VNManager.GetInstance();
        if (vnManager != null)
        {
            vnManager.ResetForMainMenu();
        }
    }

    private void HidePersistentUI()
    {
        // 获取 DontDestroyOnLoad 场景，只隐藏 Canvas 相关对象
        GameObject temp = new GameObject();
        DontDestroyOnLoad(temp);
        Scene dontDestroyScene = temp.scene;
        Destroy(temp);

        foreach (GameObject rootObj in dontDestroyScene.GetRootGameObjects())
        {
            // 只隐藏有 Canvas 组件的对象（避免影响非 UI 的 DontDestroyOnLoad 对象）
            if (rootObj.GetComponent<Canvas>() != null)
            {
                rootObj.SetActive(false);
            }
        }
    }
}
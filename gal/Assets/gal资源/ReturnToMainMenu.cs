using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMainMenu : MonoBehaviour
{
    // 在 Inspector 里可指定目标场景名，默认 "MainMenu"
    public string targetSceneName = "VNMainMenu";

    public void GoToMainMenu()
    {
        // 如果存在残留的全局 UI（DontDestroyOnLoad），可以隐藏它们
        HidePersistentUI();

        SceneManager.LoadScene(targetSceneName);
    }

    private void HidePersistentUI()
    {
        // 获取 DontDestroyOnLoad 场景，只禁用 Canvas，绝不动其他管理器
        GameObject temp = new GameObject();
        DontDestroyOnLoad(temp);
        Scene dontDestroyScene = temp.scene;
        Destroy(temp);

        foreach (GameObject rootObj in dontDestroyScene.GetRootGameObjects())
        {
            // 只处理挂有 Canvas 的物体（它们是 UI 根）
            if (rootObj.GetComponent<Canvas>() != null)
            {
                rootObj.SetActive(false);
            }
            // 如果你确定主界面需要全新的 EventSystem，也可以禁用旧的 EventSystem
            // 但要确保主界面场景自带 EventSystem
        }
    }
}
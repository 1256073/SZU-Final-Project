using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 显示设置初始化脚本
/// - 将相机背景色设为黑色（替代默认蓝边）
/// - 确保使用窗口模式（可通过Windows控件最大化），禁止全屏
/// - 允许窗口缩放
/// </summary>
public static class DisplaySetup
{
    private static bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoad()
    {
        // 强制窗口模式，禁止全屏（用户可通过Windows控件最大化）
        Screen.fullScreenMode = FullScreenMode.Windowed;

        if (!isInitialized)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            isInitialized = true;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 每个场景加载后重新设置相机背景为黑色
        SetAllCamerasBackgroundBlack();
    }

    private static void SetAllCamerasBackgroundBlack()
    {
        Camera[] cameras = Camera.allCameras;
        foreach (Camera cam in cameras)
        {
            if (cam != null)
            {
                cam.backgroundColor = Color.black;
            }
        }

        // 如果没有找到相机，尝试设置主相机
        if (cameras.Length == 0)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = Color.black;
            }
        }
    }
}

using UnityEngine;

/// <summary>
/// MainMenuBGM — 主界面 / 设置界面背景音乐管理器
/// 挂载在 VNMainMenu 场景中的 GameObject 上即可。
/// 主界面和设置界面共用同一个 BGM 播放器（DontDestroyOnLoad），
/// 音量跟随 GlobalDataManager 的 BGM 设置实时变化，便于在设置界面调音时参照。
/// </summary>
public class MainMenuBGM : MonoBehaviour
{
    [Header("【BGM 曲目】")]
    [SerializeField] private AudioClip bgmClip;

    [Header("【默认音量（全局数据不可用时回退）】")]
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 0.7f;

    private static AudioSource bgmSource;
    private static float s_defaultVolume;
    private static float s_lastKnownBGMVolume = -1f;
    private static float s_lastKnownMasterVolume = -1f;
    /// <summary>是否正在退出应用（避免 OnDestroy 时误暂停）</summary>
    private static bool s_isQuitting = false;

    [RuntimeInitializeOnLoadMethod]
    private static void RegisterQuitHandler()
    {
        Application.quitting += () => s_isQuitting = true;
    }

    private void Awake()
    {
        s_defaultVolume = defaultVolume;

        // 如果已有 BGM 播放器（从设置界面返回等情况），恢复音量和播放
        if (bgmSource != null)
        {
            SyncVolumeFromGlobalData();
            if (!bgmSource.isPlaying)
                bgmSource.UnPause();
            return;
        }

        if (bgmClip == null)
        {
            Debug.LogWarning("[MainMenuBGM] BGM Clip 未赋值，请在 Inspector 中指定。");
            return;
        }

        // 创建持久 BGM 播放器
        GameObject bgmObj = new GameObject("MainMenu_BGM_Player");
        DontDestroyOnLoad(bgmObj);
        bgmSource = bgmObj.AddComponent<AudioSource>();
        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        SyncVolumeFromGlobalData();
        bgmSource.Play();
    }

    /// <summary>
    /// 从 VNovelizer 全局设置同步 BGM 音量
    /// 可在设置界面滑块变化时调用，实现实时音量参照
    /// </summary>
    public static void SyncVolumeFromGlobalData()
    {
        if (bgmSource == null) return;
        var data = GlobalDataManager.GetInstance()?.GetGlobalData();
        if (data != null)
        {
            float target = data.BGMVolume * data.MasterVolume;
            bgmSource.volume = target;
            s_lastKnownBGMVolume = data.BGMVolume;
            s_lastKnownMasterVolume = data.MasterVolume;
        }
        else
        {
            bgmSource.volume = s_defaultVolume;
        }
    }

    private void Update()
    {
        // 每帧检测全局音量是否变化，实现设置界面调音时的实时参照
        if (bgmSource == null) return;
        var data = GlobalDataManager.GetInstance()?.GetGlobalData();
        if (data == null) return;
        if (!Mathf.Approximately(data.BGMVolume, s_lastKnownBGMVolume) ||
            !Mathf.Approximately(data.MasterVolume, s_lastKnownMasterVolume))
        {
            SyncVolumeFromGlobalData();
        }
    }

    /// <summary>
    /// 暂停 BGM（进入小游戏或剧情时调用）
    /// </summary>
    public static void Pause()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Pause();
    }

    /// <summary>
    /// 恢复 BGM（返回主菜单时调用）
    /// </summary>
    public static void Resume()
    {
        if (bgmSource != null)
        {
            SyncVolumeFromGlobalData();
            if (!bgmSource.isPlaying)
                bgmSource.UnPause();
        }
    }

    private void OnDestroy()
    {
        // 当 VNMainMenu 场景卸载（进入剧情）时，暂停 BGM 以便返回时恢复
        // 退出应用时不暂停（让系统自然清理）
        if (!s_isQuitting)
            Pause();
    }

    /// <summary>
    /// 完全停止并销毁 BGM 播放器（退出应用等场景）
    /// </summary>
    public static void DestroyBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
            Destroy(bgmSource.gameObject);
            bgmSource = null;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace PacScripts
{
    /// <summary>
    /// PacPause — 暂停功能管理器
    /// 点击暂停按钮时显示暂停界面、暂停游戏、降低 BGM
    /// 包含音量调节滑块（主音量/BGM/音效），与 VN 设置同步
    /// </summary>
    public class PacPause : MonoBehaviour
    {
        // ==================== Inspector 参数 ====================

        [Header("【暂停 UI】")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Canvas pauseCanvas;

        [Header("【音量滑块】")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("【返回剧情】")]
        [SerializeField] private Button backToVNButton;
        [SerializeField] private Button skipButton;

        // ==================== 内部状态 ====================

        private bool isPaused = false;
        private bool _slidersInitialized = false;

        // ==================== Unity 生命周期 ====================

        private void Start()
        {
            if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(false);
            if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseButtonClicked);
            if (backToVNButton != null) backToVNButton.onClick.AddListener(OnBackToVNClicked);
            if (skipButton != null) skipButton.onClick.AddListener(OnBackToVNClicked);

            // 绑定滑块
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            // 从 VN 全局设置读取初始值
            InitSlidersFromGlobalData();
        }

        private void OnDestroy()
        {
            if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
            if (backToVNButton != null) backToVNButton.onClick.RemoveListener(OnBackToVNClicked);
            if (skipButton != null) skipButton.onClick.RemoveListener(OnBackToVNClicked);
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }

        // ==================== 滑块初始化 ====================

        private void InitSlidersFromGlobalData()
        {
            var data = GlobalDataManager.GetInstance()?.GetGlobalData();
            if (data == null) return;

            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(data.MasterVolume);
            if (bgmVolumeSlider != null)
                bgmVolumeSlider.SetValueWithoutNotify(data.BGMVolume);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.SetValueWithoutNotify(data.SFXVolume);

            _slidersInitialized = true;
        }

        // ==================== 暂停逻辑 ====================

        private void OnPauseButtonClicked()
        {
            if (isPaused) return;
            isPaused = true;

            if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(true);
            Time.timeScale = 0f;

            // 刷新滑块为最新值
            InitSlidersFromGlobalData();

            // 降低 BGM 音量
            IniPac.LowerBGMVolume();
        }

        /// <summary>由 PacContinue 调用，标记暂停状态已解除</summary>
        public void MarkResumed()
        {
            isPaused = false;
        }

        // ==================== 返回剧情 ====================

        private void OnBackToVNClicked()
        {
            Time.timeScale = 1f;
            IniPac.StopBGM();

            if (SceneLoader.Instance != null && SceneLoader.Instance.IsMiniGameRunning)
                SceneLoader.Instance.UnloadMiniGame();
            else if (Jump2Pac.Instance != null)
                Jump2Pac.Instance.ReturnToMainMenu();
            else
                SceneLoader.Instance?.UnloadMiniGame();
        }

        // ==================== 音量调节（即时生效 + 写入 GlobalData） ====================

        private void OnMasterVolumeChanged(float value)
        {
            if (!_slidersInitialized) return;

            var data = GlobalDataManager.GetInstance()?.GetGlobalData();
            if (data != null)
            {
                data.MasterVolume = value;
                GlobalDataManager.GetInstance().UpdateVolumeSettings(
                    data.MasterVolume, data.BGMVolume, data.VoiceVolume, data.SFXVolume);
            }
            AudioListener.volume = value;

            // BGM 实际音量 = BGM滑块值 × 主滑块值
            float bgmVal = bgmVolumeSlider != null ? bgmVolumeSlider.value : 1f;
            IniPac.SetBGMVolume(bgmVal * value);
        }

        private void OnBGMVolumeChanged(float value)
        {
            if (!_slidersInitialized) return;

            var data = GlobalDataManager.GetInstance()?.GetGlobalData();
            if (data != null)
            {
                data.BGMVolume = value;
                GlobalDataManager.GetInstance().UpdateVolumeSettings(
                    data.MasterVolume, data.BGMVolume, data.VoiceVolume, data.SFXVolume);
            }
            float masterVal = masterVolumeSlider != null ? masterVolumeSlider.value : 1f;
            IniPac.SetBGMVolume(value * masterVal);
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (!_slidersInitialized) return;

            var data = GlobalDataManager.GetInstance()?.GetGlobalData();
            if (data != null)
            {
                data.SFXVolume = value;
                GlobalDataManager.GetInstance().UpdateVolumeSettings(
                    data.MasterVolume, data.BGMVolume, data.VoiceVolume, data.SFXVolume);
            }
        }
    }
}

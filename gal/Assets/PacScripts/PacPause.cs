using UnityEngine;
using UnityEngine.UI;

namespace PacScripts
{
    /// <summary>
    /// PacPause — 暂停功能管理器
    /// 点击暂停按钮时显示暂停界面并暂停游戏
    /// </summary>
    public class PacPause : MonoBehaviour
    {
        // ==================== Inspector 参数 ====================

        [Header("【暂停 UI】")]
        /// <summary>暂停按钮</summary>
        [SerializeField] private Button pauseButton;
        /// <summary>暂停界面 Canvas</summary>
        [SerializeField] private Canvas pauseCanvas;

        [Header("【返回剧情】")]
        /// <summary>返回剧情按钮（卸载小游戏，回到 VN）</summary>
        [SerializeField] private Button backToVNButton;
        /// <summary>跳过按钮（与继续按钮等效，直接返回剧情/主菜单）</summary>
        [SerializeField] private Button skipButton;

        // ==================== 内部状态 ====================

        /// <summary>当前是否处于暂停状态，防止重复触发</summary>
        private bool isPaused = false;

        // ==================== Unity 生命周期 ====================

        private void Start()
        {
            // 游戏开始时隐藏暂停 Canvas
            if (pauseCanvas != null)
            {
                pauseCanvas.gameObject.SetActive(false);
            }

            // 绑定暂停按钮事件
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }

            // 绑定返回剧情按钮事件
            if (backToVNButton != null)
            {
                backToVNButton.onClick.AddListener(OnBackToVNClicked);
            }

            // 绑定跳过按钮事件（与继续/返回剧情等效）
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnBackToVNClicked);
            }
        }

        private void OnDestroy()
        {
            // 清理按钮监听
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
            }
            if (backToVNButton != null)
            {
                backToVNButton.onClick.RemoveListener(OnBackToVNClicked);
            }
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(OnBackToVNClicked);
            }
        }

        // ==================== 暂停逻辑 ====================

        /// <summary>
        /// 点击暂停按钮：显示暂停 Canvas，暂停游戏
        /// 暂停期间禁止重复触发暂停
        /// </summary>
        private void OnPauseButtonClicked()
        {
            // 禁止重复触发
            if (isPaused) return;

            isPaused = true;

            // 显示暂停界面
            if (pauseCanvas != null)
            {
                pauseCanvas.gameObject.SetActive(true);
            }

            // 暂停游戏
            Time.timeScale = 0f;

            // 降低 BGM 音量
            IniPac.LowerBGMVolume();
        }

        /// <summary>
        /// 由 PacContinue 调用，标记暂停状态已解除
        /// </summary>
        public void MarkResumed()
        {
            isPaused = false;
        }

        // ==================== 返回剧情 ====================

        /// <summary>
        /// 点击返回剧情按钮：恢复时间流速，卸载小游戏场景
        /// 根据进入上下文自动选择正确的返回路径（主菜单 vs 剧情中）
        /// </summary>
        private void OnBackToVNClicked()
        {
            Time.timeScale = 1f;
            IniPac.StopBGM();

            // 判断进入上下文：SceneLoader.IsMiniGameRunning 为 true 表示从剧情进入
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsMiniGameRunning)
            {
                SceneLoader.Instance.UnloadMiniGame();
            }
            else if (Jump2Pac.Instance != null)
            {
                Jump2Pac.Instance.ReturnToMainMenu();
            }
            else
            {
                SceneLoader.Instance?.UnloadMiniGame();
            }
        }
    }
}

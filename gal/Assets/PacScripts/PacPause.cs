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
        }

        private void OnDestroy()
        {
            // 清理按钮监听
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
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
        }

        /// <summary>
        /// 由 PacContinue 调用，标记暂停状态已解除
        /// </summary>
        public void MarkResumed()
        {
            isPaused = false;
        }
    }
}

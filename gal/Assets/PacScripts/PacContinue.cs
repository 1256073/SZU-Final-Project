using UnityEngine;
using UnityEngine.UI;

namespace PacScripts
{
    /// <summary>
    /// PacContinue — 恢复游戏管理器
    /// 点击恢复按钮时隐藏暂停界面并恢复游戏
    /// 若已 GameOver 则不得恢复游戏
    /// </summary>
    public class PacContinue : MonoBehaviour
    {
        // ==================== Inspector 参数 ====================

        [Header("【恢复 UI】")]
        /// <summary>暂停界面 Canvas（用于隐藏）</summary>
        [SerializeField] private Canvas pauseCanvas;
        /// <summary>恢复按钮</summary>
        [SerializeField] private Button resumeButton;

        // ==================== 内部缓存 ====================

        /// <summary>PacOver 结算管理器引用</summary>
        private PacOver pacOver;
        /// <summary>PacPause 暂停管理器引用</summary>
        private PacPause pacPause;

        // ==================== Unity 生命周期 ====================

        private void Start()
        {
            // 初始化阶段缓存引用
            pacOver = FindFirstObjectByType<PacOver>();
            pacPause = FindFirstObjectByType<PacPause>();

            if (pacOver == null) Debug.LogWarning("PacContinue: 未找到 PacOver 组件！");

            // 绑定恢复按钮事件
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumeButtonClicked);
            }
        }

        private void OnDestroy()
        {
            // 清理按钮监听
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnResumeButtonClicked);
            }
        }

        // ==================== 恢复逻辑 ====================

        /// <summary>
        /// 点击恢复按钮：隐藏暂停 Canvas
        /// 若游戏未结算，则恢复游戏（Time.timeScale = 1）
        /// 若已 GameOver 则不得恢复游戏
        /// </summary>
        private void OnResumeButtonClicked()
        {
            // 隐藏暂停界面
            if (pauseCanvas != null)
            {
                pauseCanvas.gameObject.SetActive(false);
            }

            // 仅当游戏未结算时才恢复
            if (pacOver != null && !pacOver.IsGameOver)
            {
                Time.timeScale = 1f;

                // 恢复 BGM 满音量
                IniPac.RestoreBGMVolume();

                // 通知 PacPause 暂停状态已解除
                if (pacPause != null)
                {
                    pacPause.MarkResumed();
                }
            }
        }
    }
}

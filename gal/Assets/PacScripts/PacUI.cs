using UnityEngine;
using UnityEngine.UI;

namespace PacScripts
{
    /// <summary>
    /// PacUI — UI 显示管理器
    /// 仅负责实时刷新 UI 显示，禁止在 PacUI 中计算任何游戏逻辑
    /// </summary>
    public class PacUI : MonoBehaviour
    {
        // ==================== Inspector 参数 ====================

        [Header("【UI 文本引用】")]
        /// <summary>剩余时间显示文本</summary>
        [SerializeField] private Text remainingTimeText;
        /// <summary>糖分储存条文本（格式：当前/最大）</summary>
        [SerializeField] private Text glucoseBarText;
        /// <summary>消化糖分显示文本</summary>
        [SerializeField] private Text digestedGlucoseText;

        [Header("【UI Slider 引用】")]
        /// <summary>糖分储存 Slider</summary>
        [SerializeField] private Slider glucoseSlider;

        // ==================== 内部缓存 ====================

        /// <summary>Pacman 玩家引用缓存</summary>
        private Pacman pacman;
        /// <summary>PacOver 结算管理器引用缓存</summary>
        private PacOver pacOver;
        /// <summary>Jump2Pac 配置引用缓存</summary>
        private Jump2Pac config;

        // ==================== Unity 生命周期 ====================

        private void Start()
        {
            // 初始化阶段缓存所有引用（仅此阶段允许查找对象）
            pacman = FindFirstObjectByType<Pacman>();
            pacOver = FindFirstObjectByType<PacOver>();
            config = Jump2Pac.Instance;

            if (pacman == null) Debug.LogWarning("PacUI: 未找到 Pacman 组件！");
            if (pacOver == null) Debug.LogWarning("PacUI: 未找到 PacOver 组件！");
            if (config == null) Debug.LogWarning("PacUI: Jump2Pac.Instance 为空！");
        }

        private void Update()
        {
            RefreshUI();
        }

        // ==================== UI 刷新 ====================

        /// <summary>
        /// 实时刷新所有 UI 显示
        /// 禁止在此方法中进行任何游戏逻辑计算
        /// </summary>
        private void RefreshUI()
        {
            // 刷新剩余时间
            RefreshRemainingTime();

            // 刷新糖分储存条
            RefreshGlucoseBar();

            // 刷新消化糖分
            RefreshDigestedGlucose();
        }

        /// <summary>
        /// 刷新剩余时间显示
        /// 普通模式：显示剩余秒数
        /// 无限模式：显示 ∞
        /// </summary>
        private void RefreshRemainingTime()
        {
            if (remainingTimeText == null) return;

            if (config != null && config.UnlimitedMode)
            {
                remainingTimeText.text = "剩余时间：∞";
            }
            else if (pacOver != null)
            {
                float remaining = pacOver.RemainingTime;
                if (remaining < 0f) remaining = 0f;
                remainingTimeText.text = "剩余时间：" + Mathf.CeilToInt(remaining).ToString();
            }
        }

        /// <summary>
        /// 刷新糖分储存条文本与 Slider
        /// 文本格式：当前糖分/最大糖分（如 50/100）
        /// Slider 值：当前糖分 ÷ 最大糖分
        /// </summary>
        private void RefreshGlucoseBar()
        {
            if (pacman == null) return;

            float current = pacman.CurrentGlucose;
            float max = pacman.MaxGlucose;

            // 更新文本
            if (glucoseBarText != null)
            {
                glucoseBarText.text = Mathf.FloorToInt(current) + "/" + Mathf.FloorToInt(max);
            }

            // 更新 Slider
            if (glucoseSlider != null)
            {
                glucoseSlider.maxValue = max;
                glucoseSlider.value = current;
            }
        }

        /// <summary>
        /// 刷新消化糖分文本显示
        /// 格式：消化糖分：xxx
        /// </summary>
        private void RefreshDigestedGlucose()
        {
            if (digestedGlucoseText == null || pacman == null) return;

            digestedGlucoseText.text = "消化糖分：" + Mathf.FloorToInt(pacman.DigestedGlucose).ToString();
        }
    }
}

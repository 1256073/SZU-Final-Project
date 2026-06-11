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

        [Header("【摄像机控制】")]
        /// <summary>放大/跟随 触发按钮</summary>
        [SerializeField] private Button cameraZoomButton;
        /// <summary>目标摄像机（不填自动取 Camera.main）</summary>
        [SerializeField] private Camera targetCamera;
        /// <summary>放大后的正交尺寸（越小越近）</summary>
        [SerializeField] private float zoomedOrthoSize = 3f;
        /// <summary>缩放过渡用时（秒）</summary>
        [SerializeField] private float zoomDuration = 0.5f;

        // ==================== 内部缓存 ====================

        /// <summary>Pacman 玩家引用缓存</summary>
        private Pacman pacman;
        /// <summary>PacOver 结算管理器引用缓存</summary>
        private PacOver pacOver;
        /// <summary>Jump2Pac 配置引用缓存</summary>
        private Jump2Pac config;

        /// <summary>是否处于放大跟随模式</summary>
        private bool isZoomedIn = false;
        /// <summary>缩放计时器（秒）</summary>
        private float zoomTimer;
        /// <summary>缩放起始正交尺寸</summary>
        private float zoomStartSize;
        /// <summary>默认正交尺寸（用于还原）</summary>
        private float defaultOrthoSize;
        /// <summary>摄像机 z 轴偏移（保持 2D 纵深不变）</summary>
        private float cameraZOffset;

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

            // 摄像机初始化
            if (targetCamera == null)
                targetCamera = Camera.main;
            if (targetCamera != null)
            {
                defaultOrthoSize = targetCamera.orthographicSize;
                cameraZOffset = targetCamera.transform.position.z;
            }
            else
            {
                Debug.LogWarning("PacUI: 未找到摄像机！");
            }

            // 绑定摄像机按钮点击事件
            if (cameraZoomButton != null)
                cameraZoomButton.onClick.AddListener(ToggleCameraZoom);
        }

        private void Update()
        {
            RefreshUI();
        }

        private void LateUpdate()
        {
            if (targetCamera == null || pacman == null) return;

            // 更新缩放计时器
            if (zoomTimer < zoomDuration)
                zoomTimer += Time.deltaTime;

            // 计算缩放进度（SmoothStep 缓动）
            float t = Mathf.Clamp01(zoomTimer / zoomDuration);
            float targetSize = isZoomedIn ? zoomedOrthoSize : defaultOrthoSize;
            targetCamera.orthographicSize = Mathf.Lerp(zoomStartSize, targetSize, Mathf.SmoothStep(0f, 1f, t));

            // 放大模式下紧跟玩家
            if (isZoomedIn)
            {
                Vector3 targetPos = pacman.transform.position;
                targetPos.z = cameraZOffset;
                targetCamera.transform.position = targetPos;
            }
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

        // ==================== 摄像机控制 ====================

        /// <summary>
        /// 切换摄像机放大跟随模式
        /// 点击按钮时触发，重置缩放计时器并记录当前正交尺寸作为起点
        /// </summary>
        public void ToggleCameraZoom()
        {
            if (targetCamera == null) return;

            isZoomedIn = !isZoomedIn;
            zoomTimer = 0f;
            zoomStartSize = targetCamera.orthographicSize;
        }
    }
}

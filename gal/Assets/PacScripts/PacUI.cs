using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        [SerializeField] private TMP_Text remainingTimeText;
        [SerializeField] private TMP_Text glucoseBarText;
        [SerializeField] private TMP_Text digestedGlucoseText;
        [SerializeField] private TMP_Text playerSpeedText;
        [SerializeField] private TMP_Text enemySpeedText;

        [Header("【UI Slider 引用】")]
        /// <summary>糖分储存 Slider</summary>
        [SerializeField] private Slider glucoseSlider;
        /// <summary>糖分 Slider 填充图片（运行时自动从 fillRect 获取）</summary>
        private Image glucoseSliderFill;
        /// <summary>糖分未满时的填充颜色</summary>
        [SerializeField] private Color glucoseNormalColor = Color.white;
        /// <summary>糖分满时的填充颜色</summary>
        [SerializeField] private Color glucoseFullColor = Color.red;

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
        private Vector3 defaultCameraPos;
        private float cameraZOffset;
        private TMP_Text zoomButtonLabel;

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
                defaultCameraPos = targetCamera.transform.position;
                cameraZOffset    = defaultCameraPos.z;
            }
            else
            {
                Debug.LogWarning("PacUI: 未找到摄像机！");
            }

            // 缓存糖分 Slider 填充图片
            if (glucoseSlider != null && glucoseSlider.fillRect != null)
            {
                glucoseSliderFill = glucoseSlider.fillRect.GetComponent<Image>();
            }

            // 绑定摄像机按钮 + 缓存文字
            if (cameraZoomButton != null)
            {
                cameraZoomButton.onClick.AddListener(ToggleCameraZoom);
                zoomButtonLabel = cameraZoomButton.GetComponentInChildren<TMP_Text>();
            }
        }

        private void Update()
        {
            RefreshUI();
        }

        private void LateUpdate()
        {
            if (targetCamera == null || pacman == null) return;

            if (zoomTimer < zoomDuration)
                zoomTimer += Time.deltaTime;

            float t = Mathf.Clamp01(zoomTimer / zoomDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // 缩放
            float targetSize = isZoomedIn ? zoomedOrthoSize : defaultOrthoSize;
            targetCamera.orthographicSize = Mathf.Lerp(zoomStartSize, targetSize, smoothT);

            // 位置
            Vector3 targetPos;
            if (isZoomedIn)
            {
                targetPos = pacman.transform.position;
                targetPos.z = cameraZOffset;
            }
            else
            {
                targetPos = defaultCameraPos;
            }
            targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, targetPos, smoothT);
        }

        // ==================== UI 刷新 ====================

        /// <summary>
        /// 实时刷新所有 UI 显示
        /// 禁止在此方法中进行任何游戏逻辑计算
        /// </summary>
        private void RefreshUI()
        {
            RefreshRemainingTime();
            RefreshGlucoseBar();
            RefreshDigestedGlucose();
            RefreshPlayerSpeed();
            RefreshEnemySpeed();
        }

        /// <summary>
        /// 刷新剩余时间：格式 剩余\n时间\n数值
        /// </summary>
        private void RefreshRemainingTime()
        {
            if (remainingTimeText == null) return;

            if (config != null && config.UnlimitedMode)
            {
                remainingTimeText.text = "剩余时间\n∞";
            }
            else if (pacOver != null)
            {
                float remaining = pacOver.RemainingTime;
                if (remaining < 0f) remaining = 0f;
                remainingTimeText.text = "剩余时间\n" + Mathf.CeilToInt(remaining);
            }
        }

        /// <summary>
        /// 刷新糖分储存条：格式 当前/最大
        /// </summary>
        private void RefreshGlucoseBar()
        {
            if (pacman == null) return;

            float current = pacman.CurrentGlucose;
            float max = pacman.MaxGlucose;

            if (glucoseBarText != null)
                glucoseBarText.text = Mathf.FloorToInt(current) + "/" + Mathf.FloorToInt(max);

            if (glucoseSlider != null)
            {
                glucoseSlider.maxValue = max;
                glucoseSlider.value = current;

                // 糖分满时填充色变红
                if (glucoseSliderFill != null)
                {
                    glucoseSliderFill.color = current >= max ? glucoseFullColor : glucoseNormalColor;
                }
            }
        }

        /// <summary>
        /// 刷新消化糖分：格式 消化\n糖分\n数值
        /// </summary>
        private void RefreshDigestedGlucose()
        {
            if (digestedGlucoseText == null || pacman == null) return;
            digestedGlucoseText.text = "消化糖分\n" + Mathf.FloorToInt(pacman.DigestedGlucose);
        }

        /// <summary>
        /// 刷新玩家速度：格式 玩家\n速度\n数值
        /// </summary>
        private void RefreshPlayerSpeed()
        {
            if (playerSpeedText == null || pacman == null) return;
            playerSpeedText.text = "玩家速度\n" + pacman.MoveSpeed.ToString("F1");
        }

        /// <summary>
        /// 刷新敌人速度：格式 敌人\n速度\n数值（从 Jump2Pac 读取）</summary>
        private void RefreshEnemySpeed()
        {
            if (enemySpeedText == null || config == null) return;
            float spd = config.EnemyInitialMoveSpeed + Time.timeSinceLevelLoad * config.EnemySpeedGrowth;
            enemySpeedText.text = "敌人速度\n" + spd.ToString("F1");
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

            if (zoomButtonLabel != null)
                zoomButtonLabel.text = isZoomedIn ? "恢复视角" : "放大视角";
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace PacScripts
{
    /// <summary>
    /// PacOver — 游戏结算管理器
    /// 负责游戏计时、结算条件判断、分数计算与场景重载
    /// </summary>
    public class PacOver : MonoBehaviour
    {
        // ==================== Inspector 参数 ====================

        [Header("【结算 UI】")]
        /// <summary>结算界面 Canvas</summary>
        [SerializeField] private Canvas settlementCanvas;
        /// <summary>坚持时长显示文本</summary>
        [SerializeField] private TMP_Text surviveTimeText;
        /// <summary>增益倍数显示文本</summary>
        [SerializeField] private TMP_Text gainMultiplierText;
        /// <summary>最终分数显示文本</summary>
        [SerializeField] private TMP_Text finalScoreText;

        [Header("【重开按钮】")]
        [SerializeField] private Button restartButton;

        // ==================== 内部状态 ====================

        private float elapsedTime = 0f;
        private bool isGameOver = false;
        private float remainingTime;
        private Pacman pacman;
        private Jump2Pac config;
        private TMP_Text restartLabel;
        /// <summary>是否由敌人触发结算（失败触发）</summary>
        private bool causedByEnemy = false;

        // ==================== 公共属性 ====================

        /// <summary>游戏是否已结算</summary>
        public bool IsGameOver => isGameOver;
        /// <summary>剩余时间（秒），供 PacUI 读取</summary>
        public float RemainingTime => remainingTime;
        /// <summary>游戏已进行时间（秒）</summary>
        public float ElapsedTime => elapsedTime;

        // ==================== Unity 生命周期 ====================

        private void Awake()
        {
            // 缓存配置引用
            config = Jump2Pac.Instance;
            if (config == null)
            {
                Debug.LogError("PacOver: Jump2Pac.Instance 为空！");
            }
        }

        private void Start()
        {
            // 缓存 Pacman 引用
            pacman = FindFirstObjectByType<Pacman>();

            // 游戏开始时：确保正常时间流速
            Time.timeScale = 1f;

            // 隐藏结算界面
            if (settlementCanvas != null)
            {
                settlementCanvas.gameObject.SetActive(false);
            }

            // 初始化计时器
            elapsedTime = 0f;
            isGameOver = false;

            // 初始化剩余时间
            if (config != null)
            {
                remainingTime = config.UnlimitedMode ? float.MaxValue : config.GameTimeLimit;
            }
            else
            {
                remainingTime = 120f;
            }

            // 绑定重开按钮
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartGame);
                restartLabel = restartButton.GetComponentInChildren<TMP_Text>();
            }
        }

        private void Update()
        {
            // 已结算则不再更新
            if (isGameOver) return;

            // 更新计时器
            elapsedTime += Time.deltaTime;

            // 普通模式：更新剩余时间
            if (config != null && !config.UnlimitedMode)
            {
                remainingTime -= Time.deltaTime;

                // 检查时间是否耗尽
                if (remainingTime <= 0f)
                {
                    remainingTime = 0f;
                    GameOver();
                }
            }

            // 无限模式不进行倒计时，remainingTime 保持为一个大值
        }

        private void OnDestroy()
        {
            // 清理按钮监听
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartGame);
            }
        }

        // ==================== 游戏结算 ====================

        /// <summary>
        /// 由敌人触发结算（失败）
        /// </summary>
        public void TriggerGameOverByEnemy()
        {
            causedByEnemy = true;
            GameOver();
        }

        /// <summary>
        /// 触发游戏结算
        /// 显示结算 Canvas，暂停游戏，计算并显示分数
        /// 确保只触发一次
        /// </summary>
        public void GameOver()
        {
            // 防止重复触发
            if (isGameOver) return;

            isGameOver = true;

            // 暂停游戏
            Time.timeScale = 0f;

            // 降低 BGM 音量
            IniPac.LowerBGMVolume();

            // 判定胜负并播放对应音效
            PlayResultSound();

            // 显示结算界面
            if (settlementCanvas != null)
            {
                settlementCanvas.gameObject.SetActive(true);
            }

            // 计算并显示分数
            CalculateAndDisplayScore();
        }

        /// <summary>
        /// 判定胜负并播放音效：
        /// 普通模式：倒计时结束=成功，敌人抓到=失败
        /// 无尽模式：消化糖分=0=失败，否则=成功
        /// </summary>
        private void PlayResultSound()
        {
            bool isSuccess;
            if (config != null && config.UnlimitedMode)
            {
                float digested = pacman != null ? pacman.DigestedGlucose : 0f;
                isSuccess = digested > 0f;
            }
            else
            {
                isSuccess = !causedByEnemy;
            }

            if (isSuccess)
                IniPac.PlaySuccessSound();
            else
                IniPac.PlayFailureSound();
        }

        /// <summary>
        /// 重新开始游戏：显示加载中 → 恢复时间 → 重载场景
        /// </summary>
        public void RestartGame()
        {
            if (restartLabel != null) restartLabel.text = "加载中...";
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ==================== 分数计算 ====================

        /// <summary>
        /// 计算并显示结算数据
        /// 坚持时长 = 实际存活秒数
        /// 增益倍数 = digestedGlucose ÷ 10
        /// 最终分数 = 坚持时长 × 增益倍数
        /// </summary>
        private void CalculateAndDisplayScore()
        {
            float surviveDuration = elapsedTime;
            float digestedGlucose = pacman != null ? pacman.DigestedGlucose : 0f;
            float gainMultiplier = digestedGlucose / 100f;
            float finalScore = surviveDuration * gainMultiplier;

            // 显示坚持时长
            if (surviveTimeText != null)
            {
                surviveTimeText.text = "坚持时长：" + surviveDuration.ToString("F1");
            }

            // 显示增益倍数
            if (gainMultiplierText != null)
            {
                gainMultiplierText.text = "增益倍数：" + gainMultiplier.ToString("F2");
            }

            // 显示最终分数
            if (finalScoreText != null)
            {
                finalScoreText.text = "最终分数：" + finalScore.ToString("F1");
            }
        }
    }
}

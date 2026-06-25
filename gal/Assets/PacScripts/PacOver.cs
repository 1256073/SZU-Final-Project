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

        [Header("【返回剧情】")]
        /// <summary>返回剧情按钮（卸载小游戏，回到 VN）</summary>
        [SerializeField] private Button backToVNButton;

        [Header("【最大分数（仅无限模式）】")]
        /// <summary>最大分数展示文本</summary>
        [SerializeField] private TMP_Text maxScoreText;
        /// <summary>清空最大分数记录的按钮</summary>
        [SerializeField] private Button clearMaxScoreButton;

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

            // 重置回合计时器（修复重新游戏时敌人速度不重置的问题）
            if (config != null)
            {
                config.ResetRoundTimer();
            }

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

            // 绑定返回剧情按钮
            if (backToVNButton != null)
            {
                backToVNButton.onClick.AddListener(BackToVN);
            }

            // 绑定清空最大分数按钮
            if (clearMaxScoreButton != null)
            {
                clearMaxScoreButton.onClick.AddListener(ClearMaxScore);
            }

            // 无限模式下展示历史最大分数（进入游戏时就显示）
            RefreshMaxScoreDisplay();
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
            if (backToVNButton != null)
            {
                backToVNButton.onClick.RemoveListener(BackToVN);
            }
            if (clearMaxScoreButton != null)
            {
                clearMaxScoreButton.onClick.RemoveListener(ClearMaxScore);
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
        /// 无限模式：碰到敌人结算，消化糖分=0=失败，消化糖分&gt;0=成功
        /// 普通模式（含教学）：时间到/碰到敌人结算，碰到敌人/消化糖分=0=失败，其余=成功
        /// </summary>
        private void PlayResultSound()
        {
            bool isSuccess;
            float digested = pacman != null ? pacman.DigestedGlucose : 0f;

            if (config != null && config.UnlimitedMode)
            {
                // 无限模式：消化糖分=0 失败，否则成功
                isSuccess = digested > 0f;
            }
            else
            {
                // 普通模式（含教学）：碰到敌人 或 消化糖分=0 判定失败
                isSuccess = !causedByEnemy && digested > 0f;
            }

            if (isSuccess)
                IniPac.PlaySuccessSound();
            else
                IniPac.PlayFailureSound();
        }

        /// <summary>
        /// 重新开始游戏：先加载新场景，再卸载旧场景，保留 VN 主菜单场景
        /// 此顺序可避免 Unity 的 "Unloading the last loaded scene" 错误
        /// </summary>
        public void RestartGame()
        {
            if (restartLabel != null) restartLabel.text = "加载中...";
            Time.timeScale = 1f;

            // 销毁旧场景的玩家和敌人，避免残留碰撞体阻挡新玩家
            if (pacman != null)
            {
                // 禁用碰撞体后再销毁，防止销毁过程中触发碰撞回调
                Collider2D col = pacman.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
                Destroy(pacman.gameObject);
                pacman = null;
            }
            // 销毁所有旧敌人
            Enemy[] oldEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (Enemy e in oldEnemies)
            {
                if (e != null && e.gameObject != null)
                {
                    Collider2D col = e.GetComponent<Collider2D>();
                    if (col != null) col.enabled = false;
                    Destroy(e.gameObject);
                }
            }

            Scene currentMiniGameScene = SceneManager.GetActiveScene();
            string sceneName = currentMiniGameScene.name;

            // 先加载新场景（Additive），确保场景数 >= 2，再卸载旧场景
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                Debug.LogError($"[PacOver] 无法异步加载场景: {sceneName}");
                return;
            }

            loadOp.completed += (_) =>
            {
                // 找到新加载的场景（与旧场景同名但不同实例）
                Scene newScene = default;
                int sceneCount = SceneManager.sceneCount;
                for (int i = 0; i < sceneCount; i++)
                {
                    Scene s = SceneManager.GetSceneAt(i);
                    if (s.isLoaded && s.name == sceneName && s.handle != currentMiniGameScene.handle)
                    {
                        newScene = s;
                        break;
                    }
                }

                // 卸载旧的小游戏场景
                if (currentMiniGameScene.isLoaded)
                {
                    AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentMiniGameScene);
                    if (unloadOp == null)
                    {
                        Debug.LogWarning($"[PacOver] 无法卸载旧场景: {sceneName}（可能已是唯一场景）");
                    }
                }

                // 设置新场景为活跃
                if (newScene.IsValid() && newScene.isLoaded)
                {
                    SceneManager.SetActiveScene(newScene);
                }
                else
                {
                    newScene = SceneManager.GetSceneByName(sceneName);
                    if (newScene.IsValid() && newScene.isLoaded)
                    {
                        SceneManager.SetActiveScene(newScene);
                    }
                    else
                    {
                        Debug.LogError($"[PacOver] 重新加载场景失败: {sceneName}");
                    }
                }
            };
        }

        /// <summary>
        /// 返回剧情/主菜单：根据进入上下文自动选择正确的返回路径
        /// </summary>
        public void BackToVN()
        {
            Time.timeScale = 1f;
            IniPac.StopBGM();

            // 判断进入上下文：
            // - SceneLoader.IsMiniGameRunning = true  → 是从剧情中进入的，走 SceneLoader 返回
            // - SceneLoader.IsMiniGameRunning = false → 是从主菜单进入的，走 Jump2Pac 返回
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
                Debug.LogWarning("[PacOver] 无法确定返回路径，尝试 SceneLoader");
                SceneLoader.Instance?.UnloadMiniGame();
            }
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

            // 无限模式：记录并展示最大分数
            if (config != null && config.UnlimitedMode)
            {
                float savedMax = PlayerPrefs.GetFloat("PacMaxScore", 0f);
                if (finalScore > savedMax)
                {
                    PlayerPrefs.SetFloat("PacMaxScore", finalScore);
                    PlayerPrefs.Save();
                }
                RefreshMaxScoreDisplay();
            }
        }

        // ==================== 最大分数 ====================

        /// <summary>刷新最大分数显示（仅无限模式时有效）</summary>
        private void RefreshMaxScoreDisplay()
        {
            if (maxScoreText == null) return;

            if (config != null && config.UnlimitedMode)
            {
                float savedMax = PlayerPrefs.GetFloat("PacMaxScore", 0f);
                maxScoreText.text = "最大分数：" + savedMax.ToString("F1");
                maxScoreText.gameObject.SetActive(true);
                if (clearMaxScoreButton != null)
                    clearMaxScoreButton.gameObject.SetActive(true);
            }
            else
            {
                // 非无限模式隐藏最大分数及相关按钮
                maxScoreText.gameObject.SetActive(false);
                if (clearMaxScoreButton != null)
                    clearMaxScoreButton.gameObject.SetActive(false);
            }
        }

        /// <summary>清空最大分数记录</summary>
        public void ClearMaxScore()
        {
            PlayerPrefs.DeleteKey("PacMaxScore");
            PlayerPrefs.Save();
            RefreshMaxScoreDisplay();
        }
    }
}

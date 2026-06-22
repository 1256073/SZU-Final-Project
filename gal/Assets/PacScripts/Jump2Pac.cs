using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace PacScripts
{
    /// <summary>
    /// Jump2Pac — 小游戏唯一配置中心与场景跳转管理器
    /// 所有游戏参数均存储于此，其它脚本统一通过 Jump2Pac.Instance 读取配置
    /// 运行过程中产生的数据不得写回 Jump2Pac
    /// </summary>
    public class Jump2Pac : MonoBehaviour
    {
        // ==================== 单例 ====================

        /// <summary>全局单例引用，场景切换后仍可被其它脚本读取</summary>
        public static Jump2Pac Instance { get; private set; }

        // ==================== Inspector 参数 ====================

        [Header("【场景跳转】")]
        /// <summary>用于触发进入 Pacman 场景的 UI 按钮</summary>
        [SerializeField] private Button startButton;
        /// <summary>开始按钮的文本标签（运行时自动从 startButton 子级获取 TMP_Text）</summary>
        private TMP_Text startLabel;
        /// <summary>Jump2Pac 所在原始场景（DontDestroyOnLoad 前缓存，用于正确卸载）</summary>
        private Scene originalScene;
        /// <summary>教学模式场景名称</summary>
        [SerializeField] private string tutorialSceneName = "Tutorial";
        /// <summary>Pacman 场景名称</summary>
        [SerializeField] private string pacmanSceneName = "Pacman";

        [Header("【游戏模式】")]
        /// <summary>是否为教学模式</summary>
        [SerializeField] private bool teachingMode = false;
        /// <summary>是否为无限模式（无时间限制）</summary>
        [SerializeField] private bool unlimitedMode = false;
        /// <summary>游戏时限（秒），仅在普通模式下生效</summary>
        [SerializeField] private float gameTimeLimit = 120f;

        [Header("【玩家配置】")]
        /// <summary>玩家初始移动速度</summary>
        [SerializeField] private float playerInitialMoveSpeed = 5f;
        /// <summary>玩家糖分储存上限</summary>
        [SerializeField] private float playerMaxGlucose = 100f;

        [Header("【敌人配置】")]
        /// <summary>敌人初始移动速度</summary>
        [SerializeField] private float enemyInitialMoveSpeed = 2f;
        /// <summary>敌人速度成长值（单位/秒）</summary>
        [SerializeField] private float enemySpeedGrowth = 0.1f;
        /// <summary>敌人 Prefab 列表，启动时按顺序逐一生成</summary>
        [SerializeField] private GameObject[] enemyPrefabs;
        /// <summary>敌人生成间隔（秒）</summary>
        [SerializeField] private float enemySpawnInterval = 1.5f;

        [Header("【道具配置】")]
        /// <summary>道具 Prefab 列表，随机从中选取生成</summary>
        [SerializeField] private GameObject[] itemPrefabs;
        /// <summary>道具生成间隔（秒/个）</summary>
        [SerializeField] private float itemSpawnInterval = 3f;

        [Header("【墙壁配置】")]
        /// <summary>墙壁移动周期（秒）</summary>
        [SerializeField] private float wallMoveCycle = 3f;
        /// <summary>墙壁横向移动范围</summary>
        [SerializeField] private float wallHorizontalRange = 2f;
        /// <summary>墙壁竖向移动范围</summary>
        [SerializeField] private float wallVerticalRange = 2f;

        // ==================== 公共属性（只读，供其它脚本读取配置） ====================

        public bool TeachingMode => teachingMode;
        public bool UnlimitedMode => unlimitedMode;
        public float GameTimeLimit => gameTimeLimit;
        public float PlayerInitialMoveSpeed => playerInitialMoveSpeed;
        public float PlayerMaxGlucose => playerMaxGlucose;
        public float EnemyInitialMoveSpeed => enemyInitialMoveSpeed;
        public float EnemySpeedGrowth => enemySpeedGrowth;
        public GameObject[] EnemyPrefabs => enemyPrefabs;
        public float EnemySpawnInterval => enemySpawnInterval;
        public GameObject[] ItemPrefabs => itemPrefabs;
        public float ItemSpawnInterval => itemSpawnInterval;
        public float WallMoveCycle => wallMoveCycle;
        public float WallHorizontalRange => wallHorizontalRange;
        public float WallVerticalRange => wallVerticalRange;

        // ==================== Unity 生命周期 ====================

        private void Awake()
        {
            // 必须在 DontDestroyOnLoad 之前缓存原始场景
            // （DontDestroyOnLoad 会将对象移到持久场景，gameObject.scene 会变）
            originalScene = gameObject.scene;

            // 单例模式：确保全局唯一，且场景切换时不销毁
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 缓存按钮文本标签
            if (startButton != null)
            {
                startLabel = startButton.GetComponentInChildren<TMP_Text>();
                if (startLabel != null)
                {
                    startLabel.text = "开始挑战";
                }
                startButton.onClick.AddListener(OnStartButtonClicked);
            }
        }

        private void OnDestroy()
        {
            // 清理按钮监听
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartButtonClicked);
            }

            // 【Bug修复】清理静态单例引用，避免场景重载时新实例自我销毁
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ==================== 场景跳转 ====================

        /// <summary>
        /// 点击按钮后加载场景：教学模式 → 教学场景，否则 → 游戏场景
        /// </summary>
        private void OnStartButtonClicked()
        {
            // 修改按钮文字为加载状态
            if (startLabel != null)
            {
                startLabel.text = "加载中...";
            }
            // 禁用按钮防止重复点击
            if (startButton != null)
            {
                startButton.interactable = false;
            }

            Time.timeScale = 1f;
            string target = teachingMode ? tutorialSceneName : pacmanSceneName;

            // 使用 Additive 模式加载，避免销毁 VN 场景
            // 注意：必须用 Awake 中缓存的 originalScene，不能用 gameObject.scene
            // （因为 Jump2Pac 是 DontDestroyOnLoad，gameObject.scene 是持久场景）
            Scene sceneToUnload = originalScene;
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(target, LoadSceneMode.Additive);
            loadOp.completed += (_) =>
            {
                if (sceneToUnload.isLoaded)
                {
                    Debug.Log($"[Jump2Pac] 卸载原场景: {sceneToUnload.name}");
                    SceneManager.UnloadSceneAsync(sceneToUnload);
                }
            };
        }
    }
}

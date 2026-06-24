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
        /// <summary>按钮显示文字（可在 Inspector 中按实例定制）</summary>
        [SerializeField] private string buttonLabel = "小游戏";
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

        // ==================== 回合计时 ====================

        /// <summary>当前回合的开始时间（由 PacOver 在游戏开始时设置）</summary>
        public float RoundStartTime { get; set; } = 0f;

        /// <summary>当前回合已进行时间（秒），替代 Time.timeSinceLevelLoad 避免场景重载时计时不重置</summary>
        public float RoundElapsedTime => Time.time - RoundStartTime;

        /// <summary>重置回合计时器（新游戏/重新游戏时调用）</summary>
        public void ResetRoundTimer()
        {
            RoundStartTime = Time.time;
        }

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
                // 优先从子级查找 TMP_Text，找不到则从按钮自身查找
                startLabel = startButton.GetComponentInChildren<TMP_Text>();
                if (startLabel == null)
                {
                    startLabel = startButton.GetComponent<TMP_Text>();
                }
                if (startLabel != null)
                {
                    startLabel.text = buttonLabel;
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

        // 缓存主菜单场景中的 Canvas 和 Camera，用于进入/退出小游戏时的切换
        private Canvas _mainMenuCanvas;
        private Camera _mainMenuCamera;
        /// <summary>当前加载的小游戏场景名称（用于返回时卸载）</summary>
        private string _currentMiniGameScene;

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
            _currentMiniGameScene = target;

            // 先隐藏主菜单面板，避免其在新场景上显示
            MainMenuPanel mainMenuPanel = Object.FindFirstObjectByType<MainMenuPanel>();
            if (mainMenuPanel != null)
            {
                mainMenuPanel.HideMe();
            }

            // 禁用主菜单的 Canvas 和 Camera，避免与迷你游戏冲突
            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas c in allCanvases)
            {
                if (c.name == "VNGamePlayCanvas" || c.name.Contains("Canvas"))
                {
                    _mainMenuCanvas = c;
                    _mainMenuCanvas.enabled = false;
                    break;
                }
            }
            Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in allCameras)
            {
                if (cam.CompareTag("MainCamera") && originalScene.IsValid() &&
                    cam.gameObject.scene == originalScene)
                {
                    _mainMenuCamera = cam;
                    _mainMenuCamera.enabled = false;
                    break;
                }
            }

            // 使用 Additive 模式加载小游戏场景，但不卸载原 VNMainMenu 场景
            // 保留 VNMainMenu 场景不被卸载，UIManager.Init 会复用场景中已有的 Canvas
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(target, LoadSceneMode.Additive);
            loadOp.completed += (_) =>
            {
                Scene newScene = SceneManager.GetSceneByName(target);
                if (newScene.IsValid())
                {
                    SceneManager.SetActiveScene(newScene);
                }
                Debug.Log($"[Jump2Pac] 小游戏场景加载完成: {target}（主菜单场景保留在后台）");
            };
        }

        /// <summary>
        /// 从小游戏场景返回主菜单（由小游戏结束逻辑调用）
        /// 注意：仅适用于从主菜单进入小游戏的场景；从剧情进入请使用 SceneLoader.UnloadMiniGame()
        /// </summary>
        public void ReturnToMainMenu()
        {
            // 安全检查：如果原始主菜单场景已丢失，回退到 SceneLoader
            if (!originalScene.IsValid() || !originalScene.isLoaded)
            {
                Debug.LogWarning("[Jump2Pac] 原主菜单场景已丢失，回退到 SceneLoader 卸载");
                SceneLoader.Instance?.UnloadMiniGame();
                return;
            }

            // 确保 BGM 已停止（兜底清理）
            IniPac.StopBGM();

            // 第一步：卸载所有小游戏场景
            int sceneCount = SceneManager.sceneCount;
            for (int i = sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.isLoaded &&
                    s.name != "DontDestroyOnLoad" &&
                    s.path != originalScene.path &&
                    !s.name.Contains("VNMainMenu") &&
                    !s.name.Contains("MainMenu"))
                {
                    if (SceneManager.sceneCount <= 1) break;
                    Debug.Log($"[Jump2Pac] 卸载场景: {s.name}");
                    SceneManager.UnloadSceneAsync(s);
                }
            }

            _currentMiniGameScene = null;

            // 第二步：恢复主菜单的 Canvas
            if (_mainMenuCanvas == null)
            {
                Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Canvas c in allCanvases)
                {
                    if (c.name == "VNGamePlayCanvas")
                    {
                        _mainMenuCanvas = c;
                        break;
                    }
                }
            }
            if (_mainMenuCanvas != null)
            {
                _mainMenuCanvas.gameObject.SetActive(true);
                _mainMenuCanvas.enabled = true;
            }

            // 第三步：恢复主菜单的 Camera
            if (_mainMenuCamera == null)
            {
                Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Camera cam in allCameras)
                {
                    if (cam.CompareTag("MainCamera") && originalScene.IsValid() &&
                        cam.gameObject.scene == originalScene)
                    {
                        _mainMenuCamera = cam;
                        break;
                    }
                }
            }
            if (_mainMenuCamera != null)
            {
                _mainMenuCamera.enabled = true;
            }

            // 第四步：恢复主菜单面板
            MainMenuPanel mainMenuPanel = Object.FindFirstObjectByType<MainMenuPanel>(FindObjectsInactive.Include);
            if (mainMenuPanel != null)
            {
                mainMenuPanel.ShowMe();
            }
            else
            {
                Debug.LogWarning("[Jump2Pac] 未找到 MainMenuPanel，无法恢复主菜单");
            }

            // 第五步：恢复按钮状态
            if (startLabel != null)
            {
                startLabel.text = buttonLabel;
            }
            if (startButton != null)
            {
                startButton.interactable = true;
            }

            // 第六步：将原主菜单场景重新设为活跃场景
            if (originalScene.IsValid() && originalScene.isLoaded)
            {
                SceneManager.SetActiveScene(originalScene);
            }
        }
    }
}

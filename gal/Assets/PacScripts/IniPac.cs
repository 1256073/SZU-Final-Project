using System.Collections;
using UnityEngine;

namespace PacScripts
{
    /// <summary>
    /// IniPac — Pacman 场景初始化管理器
    /// 负责读取 Jump2Pac 配置，生成玩家、敌人、糖分、道具
    /// </summary>
    public class IniPac : MonoBehaviour
    {
        // ==================== Inspector 参数 ====================

        [Header("【场景范围】")]
        /// <summary>场景 X 轴最大值（整数），糖分将在 [-X, +X] 范围生成</summary>
        [SerializeField] private int sceneXMax = 10;
        /// <summary>场景 Y 轴最大值（整数），糖分将在 [-Y, +Y] 范围生成</summary>
        [SerializeField] private int sceneYMax = 10;

        [Header("【玩家生成】")]
        /// <summary>玩家 Prefab</summary>
        [SerializeField] private GameObject playerPrefab;

        [Header("【糖分生成】")]
        /// <summary>糖分运行时生成速度（个/秒）</summary>
        [SerializeField] private float glucoseSpawnRate = 2f;
        /// <summary>糖分 Prefab</summary>
        [SerializeField] private GameObject glucosePrefab;

        [Header("【BGM】")]
        /// <summary>BGM 曲目</summary>
        [SerializeField] private AudioClip bgmClip;
        /// <summary>结算界面时 BGM 音量倍数（0~1）</summary>
        [SerializeField] private float bgmLowVolumeMultiplier = 0.5f;

        [Header("【结算音效】")]
        /// <summary>成功音效</summary>
        [SerializeField] private AudioClip successSound;
        /// <summary>失败音效</summary>
        [SerializeField] private AudioClip failureSound;


        // ==================== 内部缓存 ====================

        /// <summary>已生成的玩家实例引用</summary>
        private GameObject playerInstance;
        /// <summary>糖分生成协程引用，用于正确停止</summary>
        private Coroutine glucoseSpawnCoroutine;
        /// <summary>道具生成协程引用，用于正确停止</summary>
        private Coroutine itemSpawnCoroutine;
        /// <summary>墙体层遮罩，用于避免生成到墙体内部</summary>
        private int wallLayerMask;

        /// <summary>BGM 播放器（静态，跨场景重载保持播放位置）</summary>
        private static AudioSource bgmAudioSource;
        /// <summary>结算界面音量倍数缓存（静态，供静态方法读取）</summary>
        private static float s_bgmLowMultiplier = 0.5f;
        /// <summary>成功/失败音效缓存（静态，供静态方法播放）</summary>
        private static AudioClip s_successSound;
        private static AudioClip s_failureSound;

        // ==================== Unity 生命周期 ====================

        private void Awake()
        {
            // 缓存墙体层遮罩（假设墙体 Layer 名为 "Wall"）
            wallLayerMask = LayerMask.GetMask("Wall");
        }

        private void Start()
        {
            // 验证 Jump2Pac 单例是否存在
            if (Jump2Pac.Instance == null)
            {
                Debug.LogError("IniPac: Jump2Pac.Instance 为空！请确保 Jump2Pac 已存在于场景中。");
                return;
            }

            // 确保游戏以正常速度运行
            Time.timeScale = 1f;

            // 1. 在整数坐标网格生成糖分
            GenerateInitialGlucose();

            // 2. 先生成玩家（在原点 (0,0)）
            SpawnPlayer();

            // 3. 按顺序逐一生成敌人（在原点 (0,0)，带间隔避免重叠）
            StartCoroutine(SpawnEnemiesSequentially());

            // 4. 启动运行时糖分生成协程
            glucoseSpawnCoroutine = StartCoroutine(SpawnGlucoseRoutine());

            // 5. 启动运行时道具生成协程
            itemSpawnCoroutine = StartCoroutine(SpawnItemsRoutine());

            // 6. 播放 BGM（如已存在则恢复播放，不会从头开始）
            PlayBGM();
        }

        private void OnDestroy()
        {
            // 正确停止所有协程
            if (glucoseSpawnCoroutine != null)
            {
                StopCoroutine(glucoseSpawnCoroutine);
                glucoseSpawnCoroutine = null;
            }
            if (itemSpawnCoroutine != null)
            {
                StopCoroutine(itemSpawnCoroutine);
                itemSpawnCoroutine = null;
            }


        }

        // ==================== 初始糖分生成 ====================

        /// <summary>
        /// 在 (-sceneXMax, +sceneXMax) 与 (-sceneYMax, +sceneYMax) 范围内，
        /// 每个整数坐标生成一个糖分 Prefab
        /// </summary>
        private void GenerateInitialGlucose()
        {
            if (glucosePrefab == null)
            {
                Debug.LogError("IniPac: glucosePrefab 未赋值！");
                return;
            }

            for (int x = -sceneXMax; x <= sceneXMax; x++)
            {
                for (int y = -sceneYMax; y <= sceneYMax; y++)
                {
                    Vector2 spawnPos = new Vector2(x, y);
                    Instantiate(glucosePrefab, spawnPos, Quaternion.identity, transform);
                }
            }
        }

        // ==================== 敌人顺序生成 ====================

        /// <summary>
        /// 延迟 enemySpawnInterval 秒后，在 (0, 0) 按列表顺序逐一生成敌人，
        /// 每次生成间隔 enemySpawnInterval 秒，避免敌人重叠
        /// </summary>
        private IEnumerator SpawnEnemiesSequentially()
        {
            Jump2Pac config = Jump2Pac.Instance;
            GameObject[] enemyPrefabs = config.EnemyPrefabs;
            float interval = config.EnemySpawnInterval;

            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("IniPac: 敌人 Prefab 列表为空，不生成敌人。");
                yield break;
            }

            // 等待首个生成间隔，给玩家缓冲时间
            yield return new WaitForSeconds(interval);

            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyPrefabs[i] != null)
                {
                    Instantiate(enemyPrefabs[i], Vector2.zero, Quaternion.identity, transform);
                }
                yield return new WaitForSeconds(interval);
            }
        }

        // ==================== 玩家生成 ====================

        /// <summary>
        /// 在原点生成玩家 Prefab
        /// </summary>
        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("IniPac: playerPrefab 未赋值！");
                return;
            }
            playerInstance = Instantiate(playerPrefab, Vector2.zero, Quaternion.identity, transform);
        }

        // ==================== 运行时糖分生成 ====================

        /// <summary>
        /// 按照糖分生成速度（个/秒）持续随机生成糖分
        /// 随机位置必须在场景范围内，且不得生成到墙体内部
        /// </summary>
        private IEnumerator SpawnGlucoseRoutine()
        {
            if (glucosePrefab == null)
            {
                Debug.LogWarning("IniPac: glucosePrefab 未赋值，无法运行时生成糖分。");
                yield break;
            }

            float spawnRate = glucoseSpawnRate;
            if (spawnRate <= 0f) yield break;

            float interval = 1f / spawnRate;

            while (true)
            {
                yield return new WaitForSeconds(interval);

                Vector2 spawnPos = GetRandomSpawnPosition();
                if (!IsPositionInsideWall(spawnPos))
                {
                    Instantiate(glucosePrefab, spawnPos, Quaternion.identity, transform);
                }
            }
        }

        // ==================== 运行时道具生成 ====================

        /// <summary>
        /// 按照道具生成间隔（秒/个）持续随机生成道具
        /// 随机位置必须在场景范围内，且不得生成到墙体内部
        /// </summary>
        private IEnumerator SpawnItemsRoutine()
        {
            Jump2Pac config = Jump2Pac.Instance;
            GameObject[] itemPrefabs = config.ItemPrefabs;
            float spawnInterval = config.ItemSpawnInterval;

            if (itemPrefabs == null || itemPrefabs.Length == 0)
            {
                Debug.LogWarning("IniPac: Jump2Pac.ItemPrefabs 列表为空，无法生成道具。");
                yield break;
            }

            if (spawnInterval <= 0f) yield break;

            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);

                Vector2 spawnPos = GetRandomSpawnPosition();
                if (!IsPositionInsideWall(spawnPos))
                {
                    // 从道具列表中随机选取一个
                    int index = Random.Range(0, itemPrefabs.Length);
                    if (itemPrefabs[index] != null)
                    {
                        Instantiate(itemPrefabs[index], spawnPos, Quaternion.identity, transform);
                    }
                }
            }
        }

        // ==================== BGM 控制 ====================

        /// <summary>
        /// 播放/恢复 BGM：首次进入创建播放器；
        /// 重新游戏时恢复音量为 1，不从头播放
        /// </summary>
        private void PlayBGM()
        {
            // 同步静态缓存（供静态方法使用）
            s_bgmLowMultiplier = bgmLowVolumeMultiplier;
            s_successSound = successSound;
            s_failureSound = failureSound;

            // 已有 BGM：恢复满音量并确保播放中
            if (bgmAudioSource != null)
            {
                bgmAudioSource.volume = 1f;
                if (!bgmAudioSource.isPlaying)
                    bgmAudioSource.UnPause();
                return;
            }

            // 无 BGM 曲目配置
            if (bgmClip == null)
                return;

            // 首次创建 BGM 播放器（DontDestroyOnLoad 跨场景保持）
            GameObject bgmObj = new GameObject("BGM_Player");
            DontDestroyOnLoad(bgmObj);
            bgmAudioSource = bgmObj.AddComponent<AudioSource>();
            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.volume = 1f;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }

        /// <summary>
        /// 降低 BGM 音量（游戏结算时调用，BGM 继续播放不暂停）
        /// </summary>
        public static void LowerBGMVolume()
        {
            if (bgmAudioSource != null && bgmAudioSource.isPlaying)
                bgmAudioSource.volume = 1f * s_bgmLowMultiplier;
        }

        /// <summary>
        /// 恢复 BGM 满音量（从暂停恢复时调用）
        /// </summary>
        public static void RestoreBGMVolume()
        {
            if (bgmAudioSource != null)
                bgmAudioSource.volume = 1f;
        }

        /// <summary>
        /// 播放成功音效（2D，不受摄像机距离影响）
        /// </summary>
        public static void PlaySuccessSound()
        {
            if (s_successSound != null)
                PlayOneShot2D(s_successSound);
        }

        /// <summary>
        /// 播放失败音效（2D，不受摄像机距离影响）
        /// </summary>
        public static void PlayFailureSound()
        {
            if (s_failureSound != null)
                PlayOneShot2D(s_failureSound);
        }

        /// <summary>
        /// 以 2D 模式播放一次性音效，确保 100% 可听见
        /// </summary>
        private static void PlayOneShot2D(AudioClip clip)
        {
            GameObject sfxObj = new GameObject("SFX_OneShot");
            AudioSource src = sfxObj.AddComponent<AudioSource>();
            src.spatialBlend = 0f; // 2D，不受距离衰减
            src.volume = 1f;
            src.PlayOneShot(clip);
            Destroy(sfxObj, clip.length + 0.1f);
        }

        /// <summary>
        /// 停止并销毁 BGM（返回主菜单等场景时调用）
        /// </summary>
        public static void StopBGM()
        {
            if (bgmAudioSource != null)
            {
                bgmAudioSource.Stop();
                Destroy(bgmAudioSource.gameObject);
                bgmAudioSource = null;
            }
        }

        // ==================== 辅助方法 ====================

        /// <summary>
        /// 获取场景范围内的随机生成位置
        /// </summary>
        private Vector2 GetRandomSpawnPosition()
        {
            float x = Random.Range(-sceneXMax, sceneXMax + 1f);
            float y = Random.Range(-sceneYMax, sceneYMax + 1f);
            return new Vector2(x, y);
        }

        /// <summary>
        /// 检测指定位置是否在墙体内部
        /// 使用小半径圆形检测，若检测到 Wall 层则返回 true
        /// </summary>
        private bool IsPositionInsideWall(Vector2 position)
        {
            float checkRadius = 0.3f;
            Collider2D hit = Physics2D.OverlapCircle(position, checkRadius, wallLayerMask);
            return hit != null;
        }
    }
}

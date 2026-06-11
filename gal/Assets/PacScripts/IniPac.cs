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

        [Header("【道具生成】")]
        /// <summary>道具生成概率（每秒概率）</summary>
        [SerializeField] private float itemSpawnProbability = 0.3f;
        /// <summary>道具 Prefab 列表</summary>
        [SerializeField] private GameObject[] itemPrefabs;

        // ==================== 内部缓存 ====================

        /// <summary>已生成的玩家实例引用</summary>
        private GameObject playerInstance;
        /// <summary>糖分生成协程引用，用于正确停止</summary>
        private Coroutine glucoseSpawnCoroutine;
        /// <summary>道具生成协程引用，用于正确停止</summary>
        private Coroutine itemSpawnCoroutine;
        /// <summary>墙体层遮罩，用于避免生成到墙体内部</summary>
        private int wallLayerMask;

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
        /// 按照每秒概率随机生成道具
        /// 随机位置必须在场景范围内，且不得生成到墙体内部
        /// </summary>
        private IEnumerator SpawnItemsRoutine()
        {
            if (itemPrefabs == null || itemPrefabs.Length == 0)
            {
                Debug.LogWarning("IniPac: itemPrefabs 列表为空，无法生成道具。");
                yield break;
            }

            while (true)
            {
                yield return new WaitForSeconds(1f);

                // 按概率判断本秒是否生成道具
                if (Random.value < itemSpawnProbability)
                {
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

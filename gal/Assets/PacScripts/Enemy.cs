using UnityEngine;

namespace PacScripts
{
    /// <summary>
    /// Enemy — 敌人 AI
    /// 行为优先级：干扰玩家 > 紧随玩家 > 随机移动
    /// 移动速度随游戏时间增长，与玩家碰撞时触发 GameOver
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        // ==================== Inspector 参数 ====================

        [Header("【引用】")]
        /// <summary>玩家对象引用（Pacman）</summary>
        [SerializeField] private Pacman player;

        [Header("【行为模式】")]
        /// <summary>是否启用随机移动模式</summary>
        [SerializeField] private bool randomMove = false;
        /// <summary>是否启用紧随玩家模式</summary>
        [SerializeField] private bool followPlayer = false;
        /// <summary>是否启用干扰玩家模式（优先级最高）</summary>
        [SerializeField] private bool harassPlayer = false;

        [Header("【干扰参数】")]
        /// <summary>干扰阈值：与玩家距离小于此值时切换为随机移动</summary>
        [SerializeField] private float harassThreshold = 3f;

        [Header("【随机移动参数】")]
        /// <summary>随机方向切换间隔（秒）</summary>
        [SerializeField] private float randomDirectionInterval = 2f;

        // ==================== 内部缓存 ====================

        /// <summary>PacOver 结算管理器引用</summary>
        private PacOver pacOver;
        /// <summary>Rigidbody2D 组件缓存</summary>
        private Rigidbody2D rb;
        /// <summary>当前敌人移动速度</summary>
        private float currentSpeed;
        /// <summary>当前随机移动方向</summary>
        private Vector2 randomDirection;
        /// <summary>随机方向计时器</summary>
        private float randomTimer;
        /// <summary>是否处于干扰模式的随机移动阶段</summary>
        private bool isHarassRandomPhase = false;
        /// <summary>是否已触发过 GameOver，防止重复调用</summary>
        private bool hasTriggeredGameOver = false;

        // ==================== Unity 生命周期 ====================

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("Enemy: 未找到 Rigidbody2D 组件！请在敌人 Prefab 上添加 Rigidbody2D。");
            }
        }

        private void Start()
        {
            // 缓存 PacOver 引用（仅初始化阶段查找）
            pacOver = FindFirstObjectByType<PacOver>();
            if (pacOver == null)
            {
                Debug.LogWarning("Enemy: 未找到 PacOver 组件！");
            }

            // 若未通过 Inspector 赋值，尝试查找玩家
            if (player == null)
            {
                player = FindFirstObjectByType<Pacman>();
                if (player == null)
                {
                    Debug.LogWarning("Enemy: 未找到 Pacman 玩家对象！");
                }
            }

            // 初始化随机方向
            randomDirection = Random.insideUnitCircle.normalized;
            randomTimer = randomDirectionInterval;
        }

        private void Update()
        {
            if (player == null) return;

            // 更新当前速度：初始速度 + 游戏已进行时间 × 速度成长值
            UpdateSpeed();

            // 根据行为优先级决定移动方向
            Vector2 moveDirection = DetermineMoveDirection();

            // 使用 Rigidbody2D 进行物理移动
            if (rb != null)
            {
                rb.velocity = moveDirection * currentSpeed;
            }
            else
            {
                // 备用：直接修改 Transform
                transform.position += (Vector3)(moveDirection * currentSpeed * Time.deltaTime);
            }
        }

        // ==================== 碰撞检测 ====================

        /// <summary>
        /// 与玩家碰撞时触发游戏结算
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!hasTriggeredGameOver && collision.gameObject.CompareTag("Player"))
            {
                TriggerGameOver();
            }
        }

        /// <summary>
        /// 与玩家触发碰撞时也触发游戏结算（兼容 Trigger 模式）
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!hasTriggeredGameOver && other.CompareTag("Player"))
            {
                TriggerGameOver();
            }
        }

        // ==================== 速度更新 ====================

        /// <summary>
        /// 根据游戏已进行时间更新敌人移动速度
        /// 公式：当前速度 = 敌人初始速度 + 游戏已进行时间 × 敌人速度成长值
        /// </summary>
        private void UpdateSpeed()
        {
            if (Jump2Pac.Instance == null) return;

            float initialSpeed = Jump2Pac.Instance.EnemyInitialMoveSpeed;
            float growth = Jump2Pac.Instance.EnemySpeedGrowth;
            float elapsedTime = Time.timeSinceLevelLoad;

            currentSpeed = initialSpeed + elapsedTime * growth;

            // 速度不能为负
            if (currentSpeed < 0f)
            {
                currentSpeed = 0f;
            }
        }

        // ==================== 行为决策 ====================

        /// <summary>
        /// 根据行为优先级决定当前移动方向
        /// 优先级：干扰玩家 > 紧随玩家 > 随机移动
        /// </summary>
        private Vector2 DetermineMoveDirection()
        {
            // 优先级1：干扰玩家
            if (harassPlayer)
            {
                return DetermineHarassDirection();
            }

            // 优先级2：紧随玩家
            if (followPlayer)
            {
                return DetermineFollowDirection();
            }

            // 优先级3：随机移动
            if (randomMove)
            {
                return DetermineRandomDirection();
            }

            // 默认：不移动
            return Vector2.zero;
        }

        /// <summary>
        /// 干扰模式方向决策：
        /// 先追踪玩家，距离小于阈值时改为随机移动，距离再次超过阈值时恢复追踪
        /// </summary>
        private Vector2 DetermineHarassDirection()
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);

            if (isHarassRandomPhase)
            {
                // 当前处于随机移动阶段
                if (distance > harassThreshold)
                {
                    // 距离再次超过阈值，恢复追踪
                    isHarassRandomPhase = false;
                    return DetermineFollowDirection();
                }
                else
                {
                    // 继续保持随机移动
                    return DetermineRandomDirection();
                }
            }
            else
            {
                // 当前处于追踪阶段
                if (distance < harassThreshold)
                {
                    // 距离小于阈值，切换为随机移动
                    isHarassRandomPhase = true;
                    return DetermineRandomDirection();
                }
                else
                {
                    // 继续追踪玩家
                    return DetermineFollowDirection();
                }
            }
        }

        /// <summary>
        /// 紧随玩家方向：指向玩家位置
        /// </summary>
        private Vector2 DetermineFollowDirection()
        {
            if (player == null) return Vector2.zero;
            Vector2 direction = (player.transform.position - transform.position).normalized;
            return direction;
        }

        /// <summary>
        /// 随机移动方向：定时切换随机方向
        /// </summary>
        private Vector2 DetermineRandomDirection()
        {
            randomTimer -= Time.deltaTime;
            if (randomTimer <= 0f)
            {
                // 生成新的随机方向
                randomDirection = Random.insideUnitCircle.normalized;
                randomTimer = randomDirectionInterval;
            }
            return randomDirection;
        }

        // ==================== 游戏结算触发 ====================

        /// <summary>
        /// 触发游戏结算，确保每个敌人只触发一次
        /// </summary>
        private void TriggerGameOver()
        {
            if (hasTriggeredGameOver) return;
            hasTriggeredGameOver = true;

            if (pacOver != null && !pacOver.IsGameOver)
            {
                pacOver.GameOver();
            }
        }
    }
}

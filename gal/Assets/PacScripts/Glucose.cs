using UnityEngine;

namespace PacScripts
{
    /// <summary>
    /// Glucose — 糖分对象
    /// 与玩家触发碰撞后追踪玩家，距离足够近时执行拾取逻辑
    /// 与墙壁碰撞时直接销毁
    /// </summary>
    public class Glucose : MonoBehaviour
    {
        // ==================== Inspector 参数 ====================

        [Header("【引用】")]
        /// <summary>玩家对象引用（Pacman）</summary>
        [SerializeField] private Pacman player;

        [Header("【追踪参数】")]
        /// <summary>与玩家的距离小于此值时执行拾取</summary>
        [SerializeField] private float disappearDistance = 0.5f;
        /// <summary>追踪玩家的移动速度</summary>
        [SerializeField] private float trackSpeed = 8f;

        [Header("【效果参数】")]
        /// <summary>拾取后增加的糖分储存值</summary>
        [SerializeField] private float glucoseAddValue = 5f;
        /// <summary>当糖分已满时，降低玩家速度的值</summary>
        [SerializeField] private float speedReduceValue = 1f;

        // ==================== 内部状态 ====================

        /// <summary>是否正在追踪玩家</summary>
        private bool isTracking = false;

        // ==================== Unity 生命周期 ====================

        private void Start()
        {
            // 若未通过 Inspector 赋值，尝试在场景中查找玩家
            if (player == null)
            {
                player = FindFirstObjectByType<Pacman>();
                if (player == null)
                {
                    Debug.LogWarning("Glucose: 未找到 Pacman 玩家对象！");
                }
            }

            // 让 Solid 碰撞器忽略玩家，避免物理推挤（Trigger 不受影响，正常检测玩家）
            IgnorePhysicsWithPlayer();
        }

        private void Update()
        {
            if (!isTracking || player == null) return;

            // 向玩家移动
            Vector2 direction = (player.transform.position - transform.position).normalized;
            transform.position += (Vector3)(direction * trackSpeed * Time.deltaTime);

            // 检查与玩家的距离
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < disappearDistance)
            {
                ExecutePickup();
            }
        }

        // ==================== 碰撞检测 ====================

        /// <summary>
        /// 触发器回调：接触玩家 → 开始追踪；接触墙壁 → 销毁自己
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                if (!isTracking)
                {
                    isTracking = true;
                    if (player == null)
                        player = other.GetComponent<Pacman>();
                }
            }
            else if (other.CompareTag("Wall"))
            {
                Destroy(gameObject);
            }
        }

        // ==================== 拾取逻辑 ====================

        /// <summary>
        /// 执行拾取逻辑：
        /// - 若 currentGlucose < maxGlucose，增加糖分
        /// - 若 currentGlucose >= maxGlucose，降低玩家速度
        /// 随后销毁自己
        /// </summary>
        private void ExecutePickup()
        {
            if (player == null)
            {
                Destroy(gameObject);
                return;
            }

            if (player.CurrentGlucose < player.MaxGlucose)
            {
                // 未满：增加糖分储存
                player.AddGlucose(glucoseAddValue);
            }
            else
            {
                // 已满：降低玩家移动速度
                player.ReduceSpeed(speedReduceValue);
            }

            // 销毁自己
            Destroy(gameObject);
        }

        // ==================== 物理隔离 ====================

        /// <summary>
        /// 让自身 Solid 碰撞器忽略玩家碰撞
        /// 这样 Trigger 照常检测玩家，Solid 只管墙壁，不会推挤 Pacman
        /// </summary>
        private void IgnorePhysicsWithPlayer()
        {
            if (player == null) return;

            Collider2D playerCol = player.GetComponent<Collider2D>();
            if (playerCol == null) return;

            foreach (var col in GetComponents<Collider2D>())
            {
                if (!col.isTrigger)
                {
                    Physics2D.IgnoreCollision(col, playerCol);
                }
            }
        }
    }
}

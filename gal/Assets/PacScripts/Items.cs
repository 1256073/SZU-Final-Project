using UnityEngine;

namespace PacScripts
{
    /// <summary>
    /// Items — 道具对象
    /// 与玩家触发碰撞后追踪玩家，距离足够近时触发多种效果
    /// 多个效果允许同时生效，执行后销毁自己
    /// </summary>
    public class Items : MonoBehaviour
    {
        // ==================== Inspector 参数 ====================

        [Header("【引用】")]
        /// <summary>玩家对象引用（Pacman）</summary>
        [SerializeField] private Pacman player;

        [Header("【追踪参数】")]
        /// <summary>与玩家的距离小于此值时触发效果</summary>
        [SerializeField] private float disappearDistance = 0.5f;
        /// <summary>追踪玩家的移动速度</summary>
        [SerializeField] private float trackSpeed = 6f;

        [Header("【效果开关】")]
        /// <summary>是否增加储存上限</summary>
        [SerializeField] private bool increaseMaxGlucose = false;
        /// <summary>是否增加移动速度</summary>
        [SerializeField] private bool increaseMoveSpeed = false;
        /// <summary>是否清空糖分储存条</summary>
        [SerializeField] private bool clearGlucoseBar = false;

        [Header("【效果数值】")]
        /// <summary>储存上限增加值</summary>
        [SerializeField] private float glucoseMaxAddValue = 20f;
        /// <summary>移动速度增加值</summary>
        [SerializeField] private float speedAddValue = 2f;
        /// <summary>糖分转换速度（将当前糖分按比例转换为移动速度）</summary>
        [SerializeField] private float glucoseToSpeedRate = 0.5f;

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
                    Debug.LogWarning("Items: 未找到 Pacman 玩家对象！");
                }
            }
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
                ExecuteEffects();
            }
        }

        // ==================== 碰撞检测 ====================

        /// <summary>
        /// 与玩家发生触发碰撞时，开始追踪玩家
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isTracking && other.CompareTag("Player"))
            {
                isTracking = true;
                // 若尚未缓存玩家引用，尝试从碰撞对象获取
                if (player == null)
                {
                    player = other.GetComponent<Pacman>();
                }
            }
        }

        /// <summary>
        /// 与墙壁碰撞时直接销毁自己
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Wall"))
            {
                Destroy(gameObject);
            }
        }

        // ==================== 效果执行 ====================

        /// <summary>
        /// 根据勾选情况执行对应效果，多个效果可同时生效
        /// 执行后销毁自己
        /// </summary>
        private void ExecuteEffects()
        {
            if (player == null)
            {
                Destroy(gameObject);
                return;
            }

            // 效果1：增加储存上限
            if (increaseMaxGlucose)
            {
                player.AddMaxGlucose(glucoseMaxAddValue);
            }

            // 效果2：增加移动速度
            if (increaseMoveSpeed)
            {
                player.AddSpeed(speedAddValue);
            }

            // 效果3：清空糖分储存条
            if (clearGlucoseBar)
            {
                player.ClearGlucose();
            }

            // 效果4：糖分转换速度 — 将当前全部糖分转换为移动速度
            // 公式：增加速度 = currentGlucose × 糖分转换速度
            // 转换后：digestedGlucose += currentGlucose，currentGlucose = 0
            if (glucoseToSpeedRate > 0f && player.CurrentGlucose > 0f)
            {
                float convertedSpeed = player.CurrentGlucose * glucoseToSpeedRate;
                player.AddSpeed(convertedSpeed);
                player.AddDigestedGlucose(player.CurrentGlucose);
                player.ClearGlucose();
            }

            // 销毁自己
            Destroy(gameObject);
        }
    }
}

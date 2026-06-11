using UnityEngine;

namespace PacScripts
{
    /// <summary>
    /// Pacman — 玩家控制器
    /// 支持 WASD 与方向键移动，管理糖分储存、消化糖分与移动速度
    /// </summary>
    public class Pacman : MonoBehaviour
    {
        // ==================== 属性 ====================

        /// <summary>当前糖分储存值，初始为 0</summary>
        [SerializeField] private float currentGlucose = 0f;
        /// <summary>最大糖分储存值，从 Jump2Pac 读取</summary>
        [SerializeField] private float maxGlucose = 100f;
        /// <summary>当前移动速度，从 Jump2Pac 读取</summary>
        [SerializeField] private float moveSpeed = 5f;
        /// <summary>累计已消化糖分，初始为 0</summary>
        [SerializeField] private float digestedGlucose = 0f;

        // ==================== 内部缓存 ====================

        /// <summary>Rigidbody2D 组件缓存</summary>
        private Rigidbody2D rb;
        /// <summary>当前帧的移动输入向量</summary>
        private Vector2 moveInput;

        // ==================== 公共属性（只读） ====================

        public float CurrentGlucose => currentGlucose;
        public float MaxGlucose => maxGlucose;
        public float MoveSpeed => moveSpeed;
        public float DigestedGlucose => digestedGlucose;
        /// <summary>当前移动输入方向（归一化）</summary>
        public Vector2 MoveDirection => moveInput;

        // ==================== Unity 生命周期 ====================

        private void Awake()
        {
            // 缓存 Rigidbody2D 组件引用
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("Pacman: 未找到 Rigidbody2D 组件！请在玩家 Prefab 上添加 Rigidbody2D。");
            }
        }

        private void Start()
        {
            // 从 Jump2Pac 读取初始配置
            if (Jump2Pac.Instance != null)
            {
                maxGlucose = Jump2Pac.Instance.PlayerMaxGlucose;
                moveSpeed = Jump2Pac.Instance.PlayerInitialMoveSpeed;
            }
            else
            {
                Debug.LogError("Pacman: Jump2Pac.Instance 为空！");
            }

            currentGlucose = 0f;
            digestedGlucose = 0f;
        }

        private void Update()
        {
            // 收集输入
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");

            // 归一化对角线移动，避免斜向速度过快
            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }
        }

        private void FixedUpdate()
        {
            // 使用 Rigidbody2D 进行物理移动
            if (rb != null)
            {
                rb.velocity = moveInput * moveSpeed;
            }
        }

        // ==================== 公共方法 ====================

        /// <summary>
        /// 增加当前糖分储存值
        /// </summary>
        /// <param name="value">增加的糖分量</param>
        public void AddGlucose(float value)
        {
            currentGlucose += value;
            // 上限钳制
            if (currentGlucose > maxGlucose)
            {
                currentGlucose = maxGlucose;
            }
        }

        /// <summary>
        /// 清空当前糖分储存值（将 currentGlucose 置零）
        /// </summary>
        public void ClearGlucose()
        {
            currentGlucose = 0f;
        }

        /// <summary>
        /// 增加当前移动速度
        /// </summary>
        /// <param name="value">增加的速度值</param>
        public void AddSpeed(float value)
        {
            moveSpeed += value;
            // 速度不能为负
            if (moveSpeed < 0f)
            {
                moveSpeed = 0f;
            }
        }

        /// <summary>
        /// 降低当前移动速度
        /// </summary>
        /// <param name="value">减少的速度值</param>
        public void ReduceSpeed(float value)
        {
            moveSpeed -= value;
            // 速度不能为负
            if (moveSpeed < 0f)
            {
                moveSpeed = 0f;
            }
        }

        /// <summary>
        /// 增加累计已消化糖分
        /// </summary>
        /// <param name="value">增加的消化糖分量</param>
        public void AddDigestedGlucose(float value)
        {
            digestedGlucose += value;
        }

        /// <summary>
        /// 增加最大糖分储存上限
        /// </summary>
        /// <param name="value">增加的上限量</param>
        public void AddMaxGlucose(float value)
        {
            maxGlucose += value;
        }
    }
}

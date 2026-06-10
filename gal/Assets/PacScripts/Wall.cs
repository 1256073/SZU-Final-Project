using UnityEngine;

namespace PacScripts
{
    /// <summary>
    /// Wall — 移动墙壁
    /// 从初始位置出发，按周期循环往返运动
    /// 水平移动与竖直移动可独立勾选
    /// </summary>
    public class Wall : MonoBehaviour
    {
        // ==================== Inspector 参数 ====================

        [Header("【移动开关】")]
        /// <summary>是否启用水平移动</summary>
        [SerializeField] private bool horizontalMove = true;
        /// <summary>是否启用竖直移动</summary>
        [SerializeField] private bool verticalMove = false;

        // ==================== 内部缓存 ====================

        /// <summary>初始位置，用于记录出发点</summary>
        private Vector2 initialPosition;
        /// <summary>当前水平移动方向（1 或 -1）</summary>
        private int horizontalDirection = 1;
        /// <summary>当前竖直移动方向（1 或 -1）</summary>
        private int verticalDirection = 1;
        /// <summary>周期计时器</summary>
        private float cycleTimer;
        /// <summary>墙壁移动周期（从 Jump2Pac 读取）</summary>
        private float moveCycle;
        /// <summary>墙壁横向移动速度（从 Jump2Pac 读取）</summary>
        private float horizontalSpeed;
        /// <summary>墙壁竖向移动速度（从 Jump2Pac 读取）</summary>
        private float verticalSpeed;

        // ==================== Unity 生命周期 ====================

        private void Start()
        {
            // 记录初始位置
            initialPosition = transform.position;

            // 从 Jump2Pac 读取配置
            if (Jump2Pac.Instance != null)
            {
                moveCycle = Jump2Pac.Instance.WallMoveCycle;
                horizontalSpeed = Jump2Pac.Instance.WallHorizontalSpeed;
                verticalSpeed = Jump2Pac.Instance.WallVerticalSpeed;
            }
            else
            {
                Debug.LogError("Wall: Jump2Pac.Instance 为空！使用默认值。");
                moveCycle = 3f;
                horizontalSpeed = 2f;
                verticalSpeed = 2f;
            }
        }

        private void Update()
        {
            // 更新周期计时器
            cycleTimer += Time.deltaTime;

            // 检查是否到达周期终点，切换方向
            if (cycleTimer >= moveCycle)
            {
                cycleTimer = 0f;
                horizontalDirection *= -1;
                verticalDirection *= -1;
            }

            // 计算本帧移动量
            Vector2 movement = Vector2.zero;

            if (horizontalMove)
            {
                movement.x = horizontalDirection * horizontalSpeed * Time.deltaTime;
            }

            if (verticalMove)
            {
                movement.y = verticalDirection * verticalSpeed * Time.deltaTime;
            }

            // 应用移动
            transform.position += (Vector3)movement;
        }
    }
}

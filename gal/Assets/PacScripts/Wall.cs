using UnityEngine;

namespace PacScripts
{
    /// <summary>
    /// Wall — 移动墙壁
    /// 以初始位置为中心，在 ±range 范围内循环往复移动
    /// </summary>
    public class Wall : MonoBehaviour
    {
        // ==================== Inspector ====================

        [Header("【移动开关】")]
        [SerializeField] private bool horizontalMove = true;
        [SerializeField] private bool verticalMove   = false;

        [Header("【行为】")]
        [SerializeField] private bool reverse = false;

        // ==================== 内部状态 ====================

        private Vector2 startPos;
        private float   cycleTimer;
        private float   moveCycle;
        private float   horizontalRange;
        private float   verticalRange;

        // ==================== 生命周期 ====================

        private void Start()
        {
            startPos = transform.position;

            var cfg = Jump2Pac.Instance;
            if (cfg != null)
            {
                moveCycle        = cfg.WallMoveCycle;
                horizontalRange  = cfg.WallHorizontalRange;
                verticalRange    = cfg.WallVerticalRange;
            }
            else
            {
                Debug.LogWarning("Wall: Jump2Pac.Instance 为空，使用默认值。");
                moveCycle        = 3f;
                horizontalRange  = 2f;
                verticalRange    = 2f;
            }
        }

        private void Update()
        {
            cycleTimer += Time.deltaTime;

            // 归一化时间 0~1，一个完整周期
            float t = (cycleTimer % moveCycle) / moveCycle;
            if (reverse) t = 1f - t;

            // 正弦波：从 -range 平滑运动到 +range
            float phase = t * Mathf.PI * 2f;

            Vector3 pos = startPos;
            if (horizontalMove) pos.x += Mathf.Sin(phase) * horizontalRange;
            if (verticalMove)   pos.y += Mathf.Sin(phase) * verticalRange;

            transform.position = pos;
        }
    }
}

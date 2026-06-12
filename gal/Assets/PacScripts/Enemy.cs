using UnityEngine;

namespace PacScripts
{
    /// <summary>
    /// Enemy — 敌人 AI（最优版）
    /// 三种模式：跟踪 / 骚扰 / 随机
    /// 含速度差异、墙壁绕行、智能反弹、反聚团
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        // ==================== Inspector ====================

        [Header("【行为模式】")]
        [SerializeField] private bool followPlayer  = false;
        [SerializeField] private bool harassPlayer  = false;
        [SerializeField] private bool randomMove    = false;

        [Header("【骚扰参数】")]
        [SerializeField] private float harassRange   = 3f;   // 骚扰范围：进入此范围后随机移动
        [SerializeField] private float harassOffset  = 2f;   // 拦截偏移：堵在玩家前方多远

        [Header("【随机参数】")]
        [SerializeField] private float visionRange   = 5f;   // 视野范围：发现玩家后切换跟踪

        [Header("【寻路参数】")]
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private float avoidDistance  = 1.2f; // 提前绕行距离
        [SerializeField] private float avoidAngleStep = 25f;  // 绕行角度步长

        // ==================== 内部状态 ====================

        private Pacman   player;
        private PacOver  pacOver;
        private Vector2  randomDir;
        private float    speedVar;          // 个体速度差异 (0.8~1.2)
        private Collider2D myCol;
        private bool     harassRandPhase;
        private bool     gameOverTriggered;

        // ==================== 生命周期 ====================

        private void Start()
        {
            pacOver = FindFirstObjectByType<PacOver>();
            if (player == null) player = FindFirstObjectByType<Pacman>();

            randomDir = Random.insideUnitCircle.normalized;
            speedVar  = Random.Range(0.8f, 1.2f);
            myCol     = GetComponent<Collider2D>();

            IgnorePickups();
        }

        private void Update()
        {
            if (player == null) return;

            // ── 速度 ──
            float speed = ReadSpeed();
            if (speed <= 0f) return;

            // ── 方向 ──
            Vector2 dir = GetDirection();
            if (dir == Vector2.zero) return;
            dir = (dir + Separation() * 0.3f).normalized;

            // ── 移动 ──
            transform.Translate(dir * speed * Time.deltaTime, Space.World);
        }

        // ==================== 速度 ====================

        private float ReadSpeed()
        {
            var cfg = Jump2Pac.Instance;
            if (cfg == null) return 0f;
            float s = (cfg.EnemyInitialMoveSpeed + Time.timeSinceLevelLoad * cfg.EnemySpeedGrowth) * speedVar;
            return s < 0f ? 0f : s;
        }

        // ==================== 碰撞 ====================

        private void OnCollisionEnter2D(Collision2D c)
        {
            if (!gameOverTriggered && c.gameObject.CompareTag("Player"))
            {
                GameOver();
            }
            else if (c.gameObject.CompareTag("Wall"))
            {
                // 法线反射 + 微扰
                Vector2 n = c.contacts[0].normal;
                randomDir = (Vector2.Reflect(randomDir, n)
                    + Random.insideUnitCircle * 0.3f).normalized;
            }
            else
            {
                // 糖分 / 道具 / 其他敌人 → 忽略
                if (myCol != null)
                    Physics2D.IgnoreCollision(myCol, c.collider, true);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!gameOverTriggered && other.CompareTag("Player"))
                GameOver();
        }

        // ==================== 方向决策 ====================

        private Vector2 GetDirection()
        {
            if (harassPlayer) return Harass();
            if (followPlayer) return Follow();
            if (randomMove)   return RandomMode();
            return Vector2.zero;
        }

        // ── 核心 ──

        private Vector2 Follow() =>
            Chase(transform.position, player.transform.position);

        private Vector2 Harass()
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);

            if (harassRandPhase)
            {
                if (dist > harassRange) harassRandPhase = false;
                else return randomDir;
            }
            else
            {
                if (dist < harassRange)
                {
                    harassRandPhase = true;
                    randomDir = Random.insideUnitCircle.normalized;
                    return randomDir;
                }
            }

            // 堵在玩家前方
            Vector2 pDir = player.MoveDirection;
            if (pDir == Vector2.zero) return Follow();
            Vector2 ahead = (Vector2)player.transform.position + pDir * harassOffset;
            return Chase(transform.position, ahead);
        }

        private Vector2 RandomMode()
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            return dist <= visionRange ? Follow() : randomDir;
        }

        // ── 寻路 ──

        /// <summary>追踪方向，遇墙自动左右绕行；全堵死则随机走</summary>
        private Vector2 Chase(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            if (d.sqrMagnitude < 0.0001f) return Vector2.zero;
            Vector2 dir = d.normalized;

            if (wallLayer == 0) return dir;

            // 起点前移，避免贴墙时自身碰撞体重叠导致漏检
            Vector2 origin = from + dir * 0.25f;
            float r = 0.35f;

            if (!Physics2D.CircleCast(origin, r, dir, avoidDistance, wallLayer))
                return dir;

            float a = avoidAngleStep;
            while (a <= 180f)
            {
                Vector2 cw  = Quaternion.Euler(0, 0,  a) * dir;
                if (!Physics2D.CircleCast(origin, r, cw, avoidDistance, wallLayer)) return cw;
                Vector2 ccw = Quaternion.Euler(0, 0, -a) * dir;
                if (!Physics2D.CircleCast(origin, r, ccw, avoidDistance, wallLayer)) return ccw;
                a += avoidAngleStep;
            }

            // 全堵住了，随机走也比撞墙好
            randomDir = Random.insideUnitCircle.normalized;
            return randomDir;
        }

        // ── 辅助 ──

        /// <summary>与其他敌人保持距离</summary>
        private Vector2 Separation()
        {
            Vector2 push = Vector2.zero;
            const float radius = 0.8f;
            var nearby = Physics2D.OverlapCircleAll(transform.position, radius, 1 << gameObject.layer);
            foreach (var c in nearby)
            {
                if (c == myCol || c.gameObject == gameObject) continue;
                Vector2 away = transform.position - c.transform.position;
                float d = away.magnitude;
                if (d > 0.01f) push += away.normalized * (radius - d) / radius;
            }
            return push;
        }

        private void IgnorePickups()
        {
            if (myCol == null) return;
            foreach (var g in FindObjectsByType<Glucose>(FindObjectsSortMode.None))
                foreach (var c in g.GetComponents<Collider2D>())
                    Physics2D.IgnoreCollision(myCol, c);
            foreach (var it in FindObjectsByType<Items>(FindObjectsSortMode.None))
                foreach (var c in it.GetComponents<Collider2D>())
                    Physics2D.IgnoreCollision(myCol, c);
        }

        // ==================== 结算 ====================

        private void GameOver()
        {
            if (gameOverTriggered) return;
            gameOverTriggered = true;
            if (pacOver != null && !pacOver.IsGameOver)
                pacOver.GameOver();
        }
    }
}


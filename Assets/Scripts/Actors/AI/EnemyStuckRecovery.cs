using UnityEngine;

namespace Game.AI
{
    /// 兜底:敌人被击退/穿模到墙外(非可走格)时,定期拉回最近可走格,避免卡死无法寻路。
    /// 飞行怪(EnemyTag.Flying)在空中飞、允许处于非可走格之上,不参与此兜底。
    public class EnemyStuckRecovery : MonoBehaviour
    {
        const float CheckInterval = 0.25f;

        float       _t;
        EnemyTag    _tag;
        Rigidbody2D _rb;

        void Awake()
        {
            _tag = GetComponent<EnemyTag>();
            _rb  = GetComponent<Rigidbody2D>();
        }

        void FixedUpdate()
        {
            if (_tag != null && _tag.Flying) return;         // 飞行怪不兜底
            if (!NavGrid.HasMap) return;

            _t += Time.fixedDeltaTime;
            if (_t < CheckInterval) return;
            _t = 0f;

            var cell = NavGrid.WorldToCell(transform.position);
            if (NavGrid.IsWalkable(cell.x, cell.y)) return;  // 仍在可走区,无需处理

            // 卡在墙体/石柱里或被打飞出界:瞬移回最近可走格,清速度。
            var safe = NavGrid.NearestWalkableWorld(transform.position);
            transform.position = new Vector3(safe.x, safe.y, transform.position.z);
            if (_rb != null) _rb.velocity = Vector2.zero;
        }
    }
}

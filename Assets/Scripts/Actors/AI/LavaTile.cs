using System.Collections.Generic;
using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    // 可走的岩浆地块（第一层）：角色可踩入，站在里面周期性掉少量血；
    // 敌人由配套的 NavHazardRegistrar 注册成危险区而绕开（故默认只伤非敌人）。
    public class LavaTile : MonoBehaviour
    {
        public float damagePerSecond = 6f;
        private const float Interval = 0.4f;
        private float _tick;
        private readonly HashSet<Collider2D> _inside = new HashSet<Collider2D>();

        private void Update()
        {
            if (_inside.Count == 0) return;
            _tick += Time.deltaTime;
            if (_tick < Interval) return;
            _tick -= Interval;

            float dmg = damagePerSecond * Interval;
            var snap = new Collider2D[_inside.Count];
            _inside.CopyTo(snap);
            foreach (var c in snap)
            {
                if (c == null) { _inside.Remove(c); continue; }
                if (EnemyTag.IsFlyingEnemy(c)) continue;   // 飞行怪在空中飞，免疫；地面怪会扣血
                c.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo
                {
                    Amount = dmg, Type = DamageType.Magical, Source = null, BypassIFrames = true,
                });
            }
        }

        private void OnTriggerEnter2D(Collider2D other) { _inside.Add(other); }
        private void OnTriggerExit2D(Collider2D other)  { _inside.Remove(other); }
    }
}

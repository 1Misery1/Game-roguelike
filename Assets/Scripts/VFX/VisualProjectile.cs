using UnityEngine;
using Game.Art;
using Game.Data;
namespace Game.VFX
{
    // 视觉专用投掷物：沿方向飞行至最大射程后自毁，不造成伤害
    // 用于为玩家弓/法杖基础攻击及线性技能提供飞行弹体视觉
    public class VisualProjectile : MonoBehaviour
    {
        Vector2 _dir;
        float   _speed;
        float   _maxRange;
        float   _traveled;

        public static void Spawn(
            ProjectileType type, Vector3 pos, Vector2 dir,
            float speed, float range, float size, Transform parent)
        {
            if (dir == Vector2.zero) dir = Vector2.right;
            dir = dir.normalized;

            var go = new GameObject("VProj_" + type);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * size;
            go.transform.rotation   = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = SkillSprites.GetProjectile(type);
            sr.color        = Color.white;
            sr.sortingOrder = 7;

            var vp = go.AddComponent<VisualProjectile>();
            vp._dir      = dir;
            vp._speed    = speed;
            vp._maxRange = range;
        }

        void Update()
        {
            float d = _speed * Time.deltaTime;
            transform.position += (Vector3)(_dir * d);
            _traveled += d;
            if (_traveled >= _maxRange) Destroy(gameObject);
        }
    }
}

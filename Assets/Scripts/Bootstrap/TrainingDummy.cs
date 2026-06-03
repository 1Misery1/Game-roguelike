using System.Collections;
using Game.Combat;
using Game.Data;
using Game.UI;
using UnityEngine;

namespace Game.Bootstrap
{
    /// 训练场木桩：血量无限，可被持续攻击。命中时按来向播放对应的"震荡"帧
    /// （左倾/右倾/后仰），随后回正；并跳伤害数字。立刻回满血——永远不倒。
    [DisallowMultipleComponent]
    public class TrainingDummy : MonoBehaviour
    {
        private const float HugeHP = 1_000_000f;

        // 帧序：0 直立 · 1 左倾 · 2 右倾 · 3 后仰 · 4 回弹/晃动
        private static Sprite[] _frames;
        private static bool     _framesTried;

        private SpriteRenderer _sr;
        private Health         _health;
        private Coroutine      _shakeCo;

        private void Awake()
        {
            LoadFrames();

            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            if (_frames != null && _frames[0] != null)
            {
                _sr.sprite = _frames[0];
                _sr.color  = Color.white;
            }
            else
            {
                // 兜底白盒
                if (_sr.sprite == null) _sr.sprite = MakeSquare();
                _sr.color = new Color(0.86f, 0.76f, 0.5f);
            }
            _sr.sortingOrder = 9;

            // 碰撞体：贴合桩身（挡住玩家，且能被普攻 OverlapCircle 命中）
            if (GetComponent<Collider2D>() == null)
            {
                float s = transform.localScale.x;
                if (s < 0.0001f) s = 1f;
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.size   = new Vector2(1.0f / s, 2.2f / s);   // 世界约 1.0 × 2.2
                box.offset = new Vector2(0f, 1.1f / s);          // 桩身位于基座之上
            }

            var stats = GetComponent<CharacterStats>();
            if (stats == null) stats = gameObject.AddComponent<CharacterStats>();
            stats.SetBase(StatType.MaxHP,   HugeHP);
            stats.SetBase(StatType.Defense, 0f);

            _health = GetComponent<Health>();
            if (_health == null) _health = gameObject.AddComponent<Health>();
            _health.OnDamaged += OnHit;
        }

        private void OnHit(DamageInfo info)
        {
            DamageNumbers.Instance?.Show(transform.position + Vector3.up * 1.6f, info.Amount, info.IsCrit);

            if (_frames != null && _frames[0] != null)
            {
                if (_shakeCo != null) StopCoroutine(_shakeCo);
                _shakeCo = StartCoroutine(ShakeRoutine(PickFrame(info.Source)));
            }
            else
            {
                if (_shakeCo != null) StopCoroutine(_shakeCo);
                _shakeCo = StartCoroutine(FlashRoutine());
            }

            _health.Heal(_health.Max);   // 无限血：立刻回满
        }

        // 根据攻击者方位选择震荡帧（木桩被推向远离攻击者的一侧）
        private int PickFrame(GameObject source)
        {
            if (source == null) return 4;
            Vector2 d = (Vector2)(transform.position - source.transform.position);
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
                return d.x >= 0f ? 2 : 1;     // 攻击者在左→右倾(2)；在右→左倾(1)
            return 3;                          // 上下方向命中→后仰(3)
        }

        private IEnumerator ShakeRoutine(int leanIdx)
        {
            if (_sr != null && _frames[leanIdx] != null) _sr.sprite = _frames[leanIdx];
            yield return new WaitForSeconds(0.10f);
            if (_sr != null && _frames[4] != null) _sr.sprite = _frames[4];   // 回弹
            yield return new WaitForSeconds(0.07f);
            if (_sr != null && _frames[0] != null) _sr.sprite = _frames[0];   // 回正
            _shakeCo = null;
        }

        private IEnumerator FlashRoutine()
        {
            if (_sr != null) _sr.color = new Color(1f, 0.45f, 0.4f);
            yield return new WaitForSeconds(0.1f);
            if (_sr != null) _sr.color = new Color(0.86f, 0.76f, 0.5f);
            _shakeCo = null;
        }

        private static void LoadFrames()
        {
            if (_framesTried) return;
            _framesTried = true;
            var f = new Sprite[5];
            bool ok = true;
            for (int i = 0; i < 5; i++)
            {
                f[i] = Resources.Load<Sprite>($"Training/Dummy_{i}");
                if (f[i] == null) ok = false;
            }
            _frames = ok ? f : null;
        }

        private static Sprite _square;
        private static Sprite MakeSquare()
        {
            if (_square != null) return _square;
            const int s = 16;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _square;
        }
    }
}

using System.Collections.Generic;
using Game.Combat;
using Game.Data;
using UnityEngine;

namespace Game.AI
{
    /// 熔岩喷发格（替代旧 LavaTile）
    /// 三阶状态机：Idle（缓慢呼吸光）→ Warning（快速橙黄脉冲 + 抖动）→ Erupting（白热核心 + 岩浆弹 + 伤害）
    /// 仅在 Erupting 阶段造成伤害，DPS 低于旧版持续扣血。
    [RequireComponent(typeof(SpriteRenderer))]
    public class LavaVent : MonoBehaviour
    {
        [SerializeField] float _damagePerSecond  = 6f;
        // 伤害半径贴合可见 ember 视觉（精灵 glow 仅到 ~0.4 格并更早淡出），避免伤害超出可见
        [SerializeField] float _eruptionRadius   = 0.4f;
        [SerializeField] float _idleDuration     = 3.5f;
        [SerializeField] float _warningDuration  = 1.2f;
        [SerializeField] float _eruptionDuration = 0.9f;
        [SerializeField] int   _dropCount        = 5;

        enum Phase { Idle, Warning, Erupting }

        Phase            _phase;
        float            _timer;
        SpriteRenderer   _sr;
        CircleCollider2D _col;
        readonly HashSet<IDamageable> _targets = new();

        // 岩浆弹对象池
        GameObject[]     _dropGOs;
        SpriteRenderer[] _dropSRs;
        Vector2[]        _dropVels;
        Vector2[]        _dropPoses;
        bool[]           _dropAlive;

        const float DropGravity = 9.0f;

        static readonly Color IdleDim    = new Color(0.35f, 0.06f, 0.01f);
        static readonly Color IdleBright = new Color(0.82f, 0.30f, 0.05f);
        static readonly Color WarnA      = new Color(1.00f, 0.38f, 0.04f);
        static readonly Color WarnB      = new Color(1.00f, 0.88f, 0.18f);
        static readonly Color EruptHot   = new Color(1.00f, 1.00f, 0.85f);
        static readonly Color EruptCool  = new Color(1.00f, 0.22f, 0.02f);

        void Awake()
        {
            _sr              = GetComponent<SpriteRenderer>();
            _sr.sprite       = MakeVentSprite();
            _sr.sortingOrder = 2;

            _col           = gameObject.AddComponent<CircleCollider2D>();
            _col.radius    = _eruptionRadius;
            _col.isTrigger = true;
            _col.enabled   = false;

            BuildDropPool();

            _phase = Phase.Idle;
            _timer = Random.Range(0f, _idleDuration); // 错开相位，避免同帧全部爆发
        }

        void BuildDropPool()
        {
            _dropGOs   = new GameObject[_dropCount];
            _dropSRs   = new SpriteRenderer[_dropCount];
            _dropVels  = new Vector2[_dropCount];
            _dropPoses = new Vector2[_dropCount];
            _dropAlive = new bool[_dropCount];

            var spr = MakeDropSprite();
            for (int i = 0; i < _dropCount; i++)
            {
                var go = new GameObject("Drop");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale    = Vector3.one * 0.5f;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = spr;
                sr.sortingOrder = 4;
                go.SetActive(false);
                _dropGOs[i] = go;
                _dropSRs[i] = sr;
            }
        }

        void Update()
        {
            _timer += Time.deltaTime;
            switch (_phase)
            {
                case Phase.Idle:     TickIdle();     break;
                case Phase.Warning:  TickWarning();  break;
                case Phase.Erupting: TickErupting(); break;
            }
        }

        void TickIdle()
        {
            float t = (Mathf.Sin(_timer * 1.5f) + 1f) * 0.5f;
            _sr.color = Color.Lerp(IdleDim, IdleBright, t);
            if (_timer >= _idleDuration)
            {
                _phase = Phase.Warning;
                _timer = 0f;
            }
        }

        void TickWarning()
        {
            float t = (Mathf.Sin(_timer * 16f) + 1f) * 0.5f;
            _sr.color = Color.Lerp(WarnA, WarnB, t);
            float jitter = 1f + Mathf.Sin(_timer * 32f) * 0.04f;
            transform.localScale = new Vector3(jitter, jitter, 1f);

            if (_timer >= _warningDuration)
            {
                _phase = Phase.Erupting;
                _timer = 0f;
                transform.localScale = Vector3.one;
                _col.enabled = true;
                LaunchDrops();
            }
        }

        void TickErupting()
        {
            float ft = _timer / _eruptionDuration;
            _sr.color = Color.Lerp(EruptHot, EruptCool, ft);

            float dt = Time.deltaTime;

            // 更新岩浆弹抛物线
            for (int i = 0; i < _dropCount; i++)
            {
                if (!_dropAlive[i]) continue;
                _dropVels[i].y -= DropGravity * dt;
                _dropPoses[i]  += _dropVels[i] * dt;
                _dropGOs[i].transform.localPosition = _dropPoses[i];
                if (_dropPoses[i].y < -1.2f)
                {
                    _dropGOs[i].SetActive(false);
                    _dropAlive[i] = false;
                }
            }

            // 伤害（仅 Erupting 阶段）
            if (_targets.Count > 0)
            {
                float dmg = _damagePerSecond * dt;
                foreach (var tgt in _targets)
                {
                    if (tgt != null)
                        tgt.TakeDamage(new DamageInfo
                        {
                            Amount        = dmg,
                            Type          = DamageType.True,
                            IsCrit        = false,
                            Source        = gameObject,
                            BypassIFrames = true,
                        });
                }
            }

            if (_timer >= _eruptionDuration)
            {
                _phase = Phase.Idle;
                _timer = 0f;
                _col.enabled = false;
                for (int i = 0; i < _dropCount; i++)
                {
                    _dropGOs[i].SetActive(false);
                    _dropAlive[i] = false;
                }
            }
        }

        void LaunchDrops()
        {
            for (int i = 0; i < _dropCount; i++)
            {
                float angle   = Random.Range(45f, 135f) * Mathf.Deg2Rad;
                float speed   = Random.Range(2.8f, 4.5f);
                _dropVels[i]  = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                _dropPoses[i] = Vector2.zero;
                _dropAlive[i] = true;
                _dropGOs[i].SetActive(true);
                _dropGOs[i].transform.localPosition = Vector3.zero;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var d = other.GetComponent<IDamageable>();
            if (d != null) _targets.Add(d);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var d = other.GetComponent<IDamageable>();
            if (d != null) _targets.Remove(d);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.color = new Color(1f, 0.45f, 0.05f, 0.55f);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, _eruptionRadius);
        }
#endif

        // ── 程序化精灵 ──────────────────────────────────────────────────────────

        static Sprite _ventSpr;
        static Sprite MakeVentSprite()
        {
            if (_ventSpr != null) return _ventSpr;
            const int SZ = 32;
            var px = new Color32[SZ * SZ];
            for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                float d = Mathf.Sqrt((x - 15.5f) * (x - 15.5f) + (y - 15.5f) * (y - 15.5f));
                if (d > 13f) { px[y * SZ + x] = new Color32(0, 0, 0, 0); continue; }

                if (d <= 8f)
                {
                    float ember = Mathf.Abs(Mathf.Sin(x * 1.9f + y * 2.6f));
                    float glow  = (8f - d) / 8f;
                    px[y * SZ + x] = new Color32(
                        (byte)(20  + 170 * ember * glow),
                        (byte)( 2  +  32 * ember * glow),
                        1, 235);
                }
                else
                {
                    float rim = (d - 8f) / 5f;
                    px[y * SZ + x] = new Color32(
                        (byte)Mathf.Lerp(228,  88, rim),
                        (byte)Mathf.Lerp( 68,  16, rim),
                        4,
                        (byte)Mathf.Lerp(245, 148, rim));
                }
            }
            var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply();
            _ventSpr = Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), 32f);
            return _ventSpr;
        }

        static Sprite _dropSpr;
        static Sprite MakeDropSprite()
        {
            if (_dropSpr != null) return _dropSpr;
            const int SZ = 8;
            var px = new Color32[SZ * SZ];
            for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                float d = Mathf.Sqrt((x - 3.5f) * (x - 3.5f) + (y - 3.5f) * (y - 3.5f));
                if (d > 3.3f) { px[y * SZ + x] = new Color32(0, 0, 0, 0); continue; }
                float t = d / 3.3f;
                px[y * SZ + x] = new Color32(
                    255,
                    (byte)Mathf.Lerp(210, 55, t),
                    (byte)Mathf.Lerp( 25,  3, t),
                    (byte)Mathf.Lerp(255, 190, t));
            }
            var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply();
            _dropSpr = Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), 32f);
            return _dropSpr;
        }
    }
}

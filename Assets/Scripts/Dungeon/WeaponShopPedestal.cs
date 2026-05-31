using Game.Data;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.UI;
namespace Game.Dungeon
{
    // 商店武器展示台 — E键购买，OnGUI显示稀有度/价格/伤害
    [RequireComponent(typeof(Collider2D))]
    public class WeaponShopPedestal : MonoBehaviour
    {
        public WeaponInstance Weapon;
        public int Price;

        public System.Func<int> GetCoins;
        public System.Action<int> SpendCoins;
        public System.Action<WeaponInstance> OnPurchased;
        public System.Action<string> ShowMessage;

        private bool _inRange;
        private bool _purchased;
        private SpriteRenderer _sr;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            _sr = GetComponent<SpriteRenderer>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null) _inRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null) _inRange = false;
        }

        private void Update()
        {
            if (_purchased || !_inRange) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.eKey.wasPressedThisFrame) TryBuy();
        }

        private void TryBuy()
        {
            int coins = GetCoins != null ? GetCoins() : 0;
            if (coins < Price)
            {
                ShowMessage?.Invoke($"Not enough coins! Need {Price} coins");
                StartCoroutine(FlashRed());
                return;
            }
            SpendCoins?.Invoke(Price);
            OnPurchased?.Invoke(Weapon);
            _purchased = true;
            Destroy(gameObject);
        }

        private System.Collections.IEnumerator FlashRed()
        {
            if (_sr == null) yield break;
            var original = _sr.color;
            _sr.color = new Color(1f, 0.2f, 0.2f);
            yield return new WaitForSeconds(0.15f);
            if (_sr != null) _sr.color = original;
        }

        private void OnGUI()
        {
            if (_purchased || Weapon == null) return;
            if (Camera.main == null) return;
            UIFonts.ApplyToSkin();

            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.9f);
            if (screen.z < 0) return;
            float x = screen.x - 100f;
            float y = Screen.height - screen.y - 20f;

            Color rc = WeaponData.GetRarityColor(Weapon.Data.rarity);
            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                normal = { textColor = rc }
            };
            GUI.Label(new Rect(x, y, 200, 18), Weapon.ShortName, nameStyle);

            var priceStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };
            GUI.Label(new Rect(x, y + 18f, 200, 18),
                $"[{Price}c]  {Weapon.EffectiveDamage:0} dmg  HP+{Weapon.HPBonus:0}", priceStyle);

            if (Weapon.Data.lifeStealRate > 0f || Weapon.Data.hpCostPerAttack > 0f)
            {
                string special = Weapon.Data.lifeStealRate > 0f
                    ? $"Lifesteal {Weapon.Data.lifeStealRate * 100:0}%"
                    : $"HP cost {Weapon.Data.hpCostPerAttack:0}/hit";
                var spStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11, alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Weapon.Data.lifeStealRate > 0f ? new Color(1f, 0.4f, 0.4f) : new Color(0.9f, 0.4f, 0.1f) }
                };
                GUI.Label(new Rect(x, y + 36f, 200, 16), special, spStyle);
            }

            if (_inRange)
            {
                var hint = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11, alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                float hintY = (Weapon.Data.lifeStealRate > 0f || Weapon.Data.hpCostPerAttack > 0f) ? y + 52f : y + 36f;
                GUI.Label(new Rect(x, hintY, 200, 16), "[E] Buy", hint);
            }
        }
    }
}

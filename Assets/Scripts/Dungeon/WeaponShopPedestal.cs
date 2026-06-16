using Game.Data;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.UI;
namespace Game.Dungeon
{
    // Weapon shop pedestal — press E to buy; shows rarity / price / damage.
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

        private WorldLabel _label;

        private void LateUpdate()
        {
            if (_purchased || Weapon == null) { _label?.Hide(); return; }
            if (_label == null) _label = gameObject.AddComponent<WorldLabel>();

            Color rc   = WeaponData.GetRarityColor(Weapon.Data.rarity);
            string rcHex   = WorldLabel.Hex(rc);
            string goldHex = WorldLabel.Hex(new Color(1f, 0.85f, 0.3f));

            string content;
            if (!_inRange)
            {
                content = $"<b><color=#{rcHex}>{Weapon.ShortName}</color></b>\n<color=#{goldHex}>{Price}c</color>";
            }
            else
            {
                content = $"<b><color=#{rcHex}>{Weapon.ShortName}</color></b>"
                    + $"\n<color=#{goldHex}>[{Price}c]   {Weapon.EffectiveDamage:0} dmg   HP+{Weapon.HPBonus:0}</color>";
                if (Weapon.Data.lifeStealRate > 0f || Weapon.Data.hpCostPerAttack > 0f)
                {
                    bool lifesteal = Weapon.Data.lifeStealRate > 0f;
                    string special = lifesteal
                        ? $"Lifesteal {Weapon.Data.lifeStealRate * 100:0}%"
                        : $"HP cost {Weapon.Data.hpCostPerAttack:0}/hit";
                    Color sc = lifesteal ? new Color(1f, 0.4f, 0.4f) : new Color(0.9f, 0.4f, 0.1f);
                    content += $"\n<color=#{WorldLabel.Hex(sc)}>{special}</color>";
                }
                content += "\n<b><color=#B3FFB3>[E] Buy</color></b>";
            }
            _label.Set(transform.position + Vector3.up * 0.95f, rc, content);
        }
    }
}

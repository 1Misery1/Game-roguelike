using Game.Data;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.UI;
namespace Game.Dungeon
{
    [RequireComponent(typeof(Collider2D))]
    public class ShopPedestal : MonoBehaviour
    {
        public TalentData talent;
        public int price = 20;
        public System.Func<int> GetCoins;
        public System.Action<int> SpendCoins;
        public System.Action<TalentData> OnPurchased;

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
            if (coins < price)
            {
                StartCoroutine(FlashRed());
                return;
            }
            SpendCoins?.Invoke(price);
            OnPurchased?.Invoke(talent);
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
            if (_purchased || talent == null) { _label?.Hide(); return; }
            if (_label == null) _label = gameObject.AddComponent<WorldLabel>();

            Color accent = new Color(1f, 0.85f, 0.3f);
            string content = $"<b><color=#{WorldLabel.Hex(accent)}>{talent.talentName}   [{price}c]</color></b>";
            if (!string.IsNullOrEmpty(talent.description)) content += $"\n<color=#CCD1E6>{talent.description}</color>";
            if (_inRange) content += "\n<color=#BFFFBF>[E] to buy</color>";
            _label.Set(transform.position + Vector3.up * 0.9f, accent, content);
        }
    }
}

using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Dungeon
{
    // 锻造台 / 附魔台 — 商店中使用，每层限定次数
    [RequireComponent(typeof(Collider2D))]
    public class ActionPedestal : MonoBehaviour
    {
        public enum ActionType { Forge, Enchant }

        public ActionType action;
        public int price;
        public int usesLeft;

        public System.Func<int> GetCoins;
        public System.Action<int> SpendCoins;
        public System.Action<string> ShowMessage;

        // Returns true if the action succeeded on a weapon, false if no valid target
        public System.Func<bool> TryApplyAction;

        private bool _inRange;
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
            if (!_inRange || usesLeft <= 0) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.eKey.wasPressedThisFrame) TryUse();
        }

        private void TryUse()
        {
            int coins = GetCoins != null ? GetCoins() : 0;
            if (coins < price)
            {
                ShowMessage?.Invoke("金币不足！");
                StartCoroutine(FlashRed());
                return;
            }
            if (TryApplyAction == null || !TryApplyAction())
                return;

            SpendCoins?.Invoke(price);
            usesLeft--;
            if (usesLeft <= 0) Destroy(gameObject);
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
            if (usesLeft <= 0) return;
            if (Camera.main == null) return;

            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.9f);
            if (screen.z < 0) return;
            float x = screen.x - 90f;
            float y = Screen.height - screen.y - 20f;

            bool isForge = action == ActionType.Forge;
            string label = isForge ? "锻造台" : "附魔台";
            Color labelColor = isForge ? new Color(1f, 0.65f, 0.2f) : new Color(0.6f, 0.4f, 1f);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                normal = { textColor = labelColor }
            };
            GUI.Label(new Rect(x, y, 180, 20), $"{label}  [{price}c]  剩{usesLeft}次", titleStyle);

            if (_inRange)
            {
                var hint = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12, alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                GUI.Label(new Rect(x, y + 20f, 180, 18), "[E] 使用 (当前武器优先)", hint);
            }
        }
    }
}

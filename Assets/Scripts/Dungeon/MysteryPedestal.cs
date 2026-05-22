using Game.Dev;
using Game.Player;
using UnityEngine;

namespace Game.Dungeon
{
    public enum MysteryOutcome
    {
        Lucky,   // +25 coins
        Gift,    // free random talent
        Heal,    // full heal
        Cursed   // -15% Max HP
    }

    [RequireComponent(typeof(Collider2D))]
    public class MysteryPedestal : MonoBehaviour
    {
        public System.Action<MysteryOutcome> OnResolved;
        private bool _resolved;
        private bool _playerInside;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            transform.Rotate(0f, 0f, 80f * Time.deltaTime);

            // 战斗期间不可交互；玩家仍站在祭坛上时，战斗结束后立即结算
            if (_playerInside) TryResolve();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            _playerInside = true;
            TryResolve();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null) _playerInside = false;
        }

        private void TryResolve()
        {
            if (_resolved || GameBootstrap.CombatInProgress) return;
            _resolved = true;
            int count = System.Enum.GetValues(typeof(MysteryOutcome)).Length;
            var outcome = (MysteryOutcome)Random.Range(0, count);
            OnResolved?.Invoke(outcome);
            Destroy(gameObject);
        }
    }
}

using Game.Data;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.UI;
namespace Game.Dungeon
{
    [RequireComponent(typeof(Collider2D))]
    public class WeaponPedestal : MonoBehaviour
    {
        public WeaponInstance          Weapon;
        public System.Action<WeaponInstance> OnEquipped;

        private bool _inRange;
        private bool _taken;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
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
            if (_taken || !_inRange) return;
            if (Keyboard.current?.eKey.wasPressedThisFrame == true)
            {
                _taken = true;
                OnEquipped?.Invoke(Weapon);
                Destroy(gameObject);
            }
        }

        private WorldLabel _label;

        private void LateUpdate()
        {
            if (_taken || Weapon?.Data == null) { _label?.Hide(); return; }
            if (_label == null) _label = gameObject.AddComponent<WorldLabel>();

            var data = Weapon.Data;
            Color rc = WeaponData.GetRarityColor(data.rarity);
            string name = $"<b><color=#{WorldLabel.Hex(rc)}>{data.weaponName}  [{Weapon.CategoryLabel}]</color></b>";

            string content;
            if (!_inRange)
            {
                content = name;   // 远处:只显示名称
            }
            else
            {
                content = name
                    + $"\n<color=#D9E6FF>{Weapon.EffectiveDamage:0} dmg   {data.attackSpeed:0.0}/s</color>";
                if (data.HasSkill) content += $"\n<color=#A6D9FF>Skill: {data.skill.skillName}</color>";
                content += "\n<b><color=#B3FFB3>[E] Equip</color></b>";
            }
            _label.Set(transform.position + Vector3.up * 0.95f, rc, content);
        }
    }
}

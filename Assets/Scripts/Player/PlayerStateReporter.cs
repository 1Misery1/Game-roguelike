using UnityEngine;

namespace Game.Player
{
    // Singleton placed on the player. Enemy AI polls this to detect
    // when the player is channelling an ultimate skill.
    public class PlayerStateReporter : MonoBehaviour
    {
        public static PlayerStateReporter Instance { get; private set; }

        // True during the cast-channel window of the hero ultimate (set by HeroActiveSkillHandler)
        public bool IsCasting { get; internal set; }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnDisable()
        {
            IsCasting = false;
        }
    }
}

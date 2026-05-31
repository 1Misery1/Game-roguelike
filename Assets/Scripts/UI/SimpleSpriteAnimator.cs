using UnityEngine;

namespace Game.UI
{
    /// 在 SpriteRenderer 上循环切换帧，挂在篝火等需要简单循环动画的物体上。
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleSpriteAnimator : MonoBehaviour
    {
        public Sprite[] frames;
        public float frameDuration = 0.12f;
        public bool unscaledTime = true;

        private SpriteRenderer _sr;
        private float _t;
        private int _i;

        private void Awake() { _sr = GetComponent<SpriteRenderer>(); }

        private void Update()
        {
            if (frames == null || frames.Length == 0) return;
            _t += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (_t >= frameDuration)
            {
                _t -= frameDuration;
                _i = (_i + 1) % frames.Length;
                _sr.sprite = frames[_i];
            }
        }
    }
}

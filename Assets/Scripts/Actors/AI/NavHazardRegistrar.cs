using UnityEngine;

namespace Game.AI
{
    /// 挂在任何危险物体上，自动在 NavGrid 注册 / 注销动态危险代价。
    /// AI 会绕开此格而不是径直走过去。
    /// 用法：实例化危险物体（如 LavaPool）时 AddComponent 并设 radius，其余自动。
    [DisallowMultipleComponent]
    public class NavHazardRegistrar : MonoBehaviour
    {
        [Tooltip("危险半径（世界单位）")]
        public float radius = 1f;

        Vector2 _registeredPos;
        float   _registeredRadius;
        bool    _registered;

        void OnEnable()
        {
            _registeredPos    = transform.position;
            _registeredRadius = Mathf.Max(0.1f, radius);
            NavGrid.AddDynamicHazard(_registeredPos, _registeredRadius);
            _registered = true;
        }

        void OnDisable()
        {
            if (!_registered) return;
            NavGrid.RemoveDynamicHazard(_registeredPos, _registeredRadius);
            _registered = false;
        }
    }
}

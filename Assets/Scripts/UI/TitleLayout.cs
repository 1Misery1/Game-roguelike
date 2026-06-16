using UnityEngine;

namespace Game.UI
{
    /// Visual layout anchors for the title screen. Lives on an empty object in the Title scene;
    /// the title UI reads these anchors at runtime to position the title text and buttons.
    ///
    /// The screen is treated as a virtual rect centred on this object with size screenSize:
    /// left→right = screen x 0→1, top→bottom = screen y 0→1 (y grows downward). Drag the child
    /// anchors onto their target spots inside the rect in the Scene view.
    /// When anchors are missing, the title falls back to a built-in layout.
    public class TitleLayout : MonoBehaviour
    {
        public enum Btn { Continue = 0, NewGame = 1, LoadSave = 2, Settings = 3, Credits = 4, Quit = 5 }

        [Header("虚拟屏幕矩形（世界单位，本物体为中心）")]
        public Vector2 screenSize = new Vector2(16f, 9f);

        [Header("锚点（拖动即可调位置）")]
        public Transform titleAnchor;
        public Transform subtitleAnchor;
        [Tooltip("顺序：Continue, New Game, Load Save, Settings, Credits, Quit")]
        public Transform[] buttonAnchors = new Transform[6];

        [Header("按钮尺寸（像素，运行时用 + 轮廓显示）")]
        public float buttonWidth  = 280f;
        public float buttonHeight = 46f;

        // 参考分辨率：仅用于在 Scene 里把像素尺寸的按钮画成轮廓
        private static readonly Vector2 RefRes = new Vector2(1920f, 1080f);

        // ── 世界锚点 → 归一化屏幕坐标(0..1, y 向下) ───────────────────
        public Vector2 Normalized(Transform t)
        {
            Vector3 c = transform.position;
            float nx = (t.position.x - c.x) / Mathf.Max(0.0001f, screenSize.x) + 0.5f;
            float ny = 0.5f - (t.position.y - c.y) / Mathf.Max(0.0001f, screenSize.y);
            return new Vector2(Mathf.Clamp01(nx), Mathf.Clamp01(ny));
        }

        // 锚点 → 屏幕像素中心
        public Vector2 ToScreenCenter(Transform t, float sw, float sh)
        {
            var n = Normalized(t);
            return new Vector2(n.x * sw, n.y * sh);
        }

        public bool HasTitle => titleAnchor != null;
        public bool HasSubtitle => subtitleAnchor != null;

        /// 第 i 个按钮的屏幕 Rect（中心对齐到锚点）；锚点缺失返回 null。
        public Rect? ButtonRect(int i, float sw, float sh)
        {
            if (buttonAnchors == null || i < 0 || i >= buttonAnchors.Length || buttonAnchors[i] == null)
                return null;
            Vector2 ctr = ToScreenCenter(buttonAnchors[i], sw, sh);
            return new Rect(ctr.x - buttonWidth * 0.5f, ctr.y - buttonHeight * 0.5f, buttonWidth, buttonHeight);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 c = transform.position;
            float W = screenSize.x, H = screenSize.y;

            // 虚拟屏幕外框
            Gizmos.color = new Color(0.5f, 0.6f, 0.9f, 0.9f);
            Gizmos.DrawWireCube(c, new Vector3(W, H, 0f));
            UnityEditor.Handles.color = new Color(0.6f, 0.7f, 1f, 1f);
            UnityEditor.Handles.Label(c + new Vector3(-W * 0.5f, H * 0.5f + 0.3f, 0f), "Title Screen (virtual)");

            // 标题 / 副标题
            DrawTextAnchor(titleAnchor, new Color(1f, 0.85f, 0.3f), "Title");
            DrawTextAnchor(subtitleAnchor, new Color(0.7f, 0.7f, 0.78f), "Subtitle");

            // 按钮轮廓 + 标签
            string[] names = { "Continue", "New Game", "Load Save", "Settings", "Credits", "Quit" };
            float boxW = buttonWidth  / RefRes.x * W;
            float boxH = buttonHeight / RefRes.y * H;
            if (buttonAnchors != null)
            {
                for (int i = 0; i < buttonAnchors.Length; i++)
                {
                    var t = buttonAnchors[i];
                    if (t == null) continue;
                    Gizmos.color = new Color(0.5f, 0.85f, 1f, 0.95f);
                    Gizmos.DrawWireCube(t.position, new Vector3(boxW, boxH, 0f));
                    UnityEditor.Handles.color = new Color(0.7f, 0.9f, 1f, 1f);
                    string lbl = i < names.Length ? names[i] : ("Btn " + i);
                    UnityEditor.Handles.Label(t.position + new Vector3(-boxW * 0.5f, boxH * 0.5f + 0.2f, 0f), lbl);
                }
            }
        }

        private void DrawTextAnchor(Transform t, Color col, string label)
        {
            if (t == null) return;
            Gizmos.color = col;
            float r = 0.35f;
            Gizmos.DrawLine(t.position + Vector3.left * r, t.position + Vector3.right * r);
            Gizmos.DrawLine(t.position + Vector3.up * r, t.position + Vector3.down * r);
            Gizmos.DrawWireCube(t.position, new Vector3(screenSize.x * 0.6f, screenSize.y * 0.06f, 0f));
            UnityEditor.Handles.color = col;
            UnityEditor.Handles.Label(t.position + new Vector3(0.2f, 0.4f, 0f), label);
        }
#endif
    }
}

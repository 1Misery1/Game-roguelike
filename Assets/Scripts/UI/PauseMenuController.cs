using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core;

namespace Game.UI
{
    /// ESC 暂停菜单。挂在 PauseMenuCanvas 上：ESC 切换暂停、Time.timeScale=0/1、
    /// 「Resume」继续、「Main Menu」返回营地。菜单本体(panel)是真实 uGUI 物件，
    /// 编辑器场景里可见可编辑；运行时默认隐藏，暂停时显示。
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;   // 菜单根（含背景/面板/按钮）
        private bool _paused;

        public bool IsPaused => _paused;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);   // 运行时默认隐藏
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                Toggle();
        }

        public void Toggle() { if (_paused) Resume(); else Pause(); }

        public void Pause()
        {
            _paused = true;
            if (panel != null) panel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            _paused = false;
            if (panel != null) panel.SetActive(false);
            Time.timeScale = 1f;
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            _paused = false;
            var gm = GameManager.Instance;
            if (gm != null) gm.ReturnToMenu();
            else SceneManager.LoadScene("Hub");
        }

        /// Quit the application (returns to the OS). Stops play mode inside the editor.
        public void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

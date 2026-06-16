using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Game.Core;
using Game.Data;

namespace Game.UI
{
    /// Title screen. Lives on the TitleCanvas in the Title scene; drives the main menu
    /// plus the settings / credits / new-game-confirm panels.
    public class TitleScreenUGUI : MonoBehaviour
    {
        private const string HubScene = "Hub";

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject confirmPanel;

        [Header("Widgets")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle fullscreenToggle;

        private void Start()
        {
            ShowMain();
            if (continueButton != null) continueButton.interactable = PersistentState.SaveExists;

            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(v => AudioListener.volume = v);
            }
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
                fullscreenToggle.onValueChanged.AddListener(v => Screen.fullScreen = v);
            }
        }

        private void ShowOnly(GameObject p)
        {
            if (mainPanel     != null) mainPanel.SetActive(p == mainPanel);
            if (settingsPanel != null) settingsPanel.SetActive(p == settingsPanel);
            if (creditsPanel  != null) creditsPanel.SetActive(p == creditsPanel);
            if (confirmPanel  != null) confirmPanel.SetActive(p == confirmPanel);
        }

        // ── 按钮回调（在 Inspector 的 OnClick 里绑定）─────────────────
        public void ShowMain()     => ShowOnly(mainPanel);
        public void ShowSettings() => ShowOnly(settingsPanel);
        public void ShowCredits()  => ShowOnly(creditsPanel);

        public void OnContinue()
        {
            if (PersistentState.SaveExists) SceneManager.LoadScene(HubScene);
        }

        public void OnNewGame()
        {
            if (PersistentState.SaveExists) ShowOnly(confirmPanel);   // 有存档先确认
            else DoNewGame();
        }

        public void OnConfirmNewGame() => DoNewGame();

        private void DoNewGame()
        {
            var ps = new PersistentState();
            var db = Resources.Load<HeroDatabase>("Heroes/HeroDatabase");   // 解锁初始英雄
            if (db != null && db.heroes != null && db.heroes.Length > 0 &&
                !ps.IsHeroUnlocked(db.heroes[0].heroName))
                ps.UnlockedHeroIds.Add(db.heroes[0].heroName);
            ps.Save();
            IntroController.ClearSeen();
            SceneManager.LoadScene(HubScene);
        }

        public void OnQuit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}

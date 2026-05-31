using Game.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public RunState       Run        { get; private set; } = new RunState();
        public PersistentState Persistent { get; private set; }

        [SerializeField] private string menuSceneName    = "Hub";
        [SerializeField] private string dungeonSceneName = "Test";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Persistent = PersistentState.Load();
        }

        public void StartRun(HeroData hero)
        {
            Run.Begin(hero);
            SceneManager.LoadScene(dungeonSceneName);
        }

        /// Called by GameBootstrap on run end (death / all floors cleared).
        public void EndRun(bool cleared)
        {
            Run.End();
            SceneManager.LoadScene(menuSceneName);
        }

        /// Shortcut for returning to menu without modifying run state (e.g. mid-floor-complete).
        public void ReturnToMenu()
        {
            Run.End();
            SceneManager.LoadScene(menuSceneName);
        }
    }
}

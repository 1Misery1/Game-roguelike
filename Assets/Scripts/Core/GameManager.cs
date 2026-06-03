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

        [SerializeField] private string menuSceneName     = "Hub";
        [SerializeField] private string dungeonSceneName  = "Test";
        [SerializeField] private string trainingSceneName = "Training";

        /// 进入训练场时所附身的英雄（训练场只用它生成可操控的真身，不开启正式 Run）。
        public HeroData TrainingHero { get; private set; }

        /// 刚从训练场返回营地的标志：大厅据此让玩家以英雄形态、出现在练武场门旁。
        public bool ReturningFromTraining { get; set; }

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

        /// 进入训练场（白盒练武场）。不开启正式 Run，只记录要试练的英雄。
        public void EnterTraining(HeroData hero)
        {
            TrainingHero = hero;
            SceneManager.LoadScene(trainingSceneName);
        }

        /// 直接进入训练场（不经营地）时补登记当前试练英雄，使返回营地能恢复附身态。
        public void SetTrainingHero(HeroData hero) => TrainingHero = hero;

        /// 从训练场返回营地。返回后大厅会让玩家以刚才操控的英雄形态、立于练武场门旁。
        public void ReturnToHub()
        {
            ReturningFromTraining = true;
            SceneManager.LoadScene(menuSceneName);
        }
    }
}

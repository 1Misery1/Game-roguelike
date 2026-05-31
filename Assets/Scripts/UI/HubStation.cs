using UnityEngine;

namespace Game.UI
{
    public enum HubStationKind { Campfire, HeroPedestal, QuestBoard, LiftDoor, Memorial, Records }

    /// 大厅交互站点：挂在场景里的物体上，在 Inspector 配置类型/英雄序号/标题。
    /// HubController 启动时收集场景中所有 HubStation 来驱动走近-按 E 的交互。
    public class HubStation : MonoBehaviour
    {
        public HubStationKind kind = HubStationKind.Campfire;
        [Tooltip("仅 HeroPedestal 使用：对应 HeroDatabase.heroes 的下标")]
        public int heroIndex = -1;
        [Tooltip("站点上方显示的标题")]
        public string title = "";
    }
}

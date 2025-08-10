

namespace Levels.UnityExtends.SceneManagement.IoC {
    
    using Levels.Core;
    using Levels.UnityExtends.SceneManagement;
    
    public class Broadcaster_Scene_OnSceneActivating : Notification.Broadcaster.Generic<World.Scene.Notifications.SceneActivating, Levels.UnityExtends.SceneManagement.World.Scene.Management.SceneActivatingProcess>
    {
        public override void SetupEventHooks() {
            Levels.UnityExtends.SceneManagement.World.Scene.Management.Events.OnSceneActivating+= HandleOnPerformed;
        }

        public override void BreakdownEventHooks() {
            Levels.UnityExtends.SceneManagement.World.Scene.Management.Events.OnSceneActivating -= HandleOnPerformed;
        }
    }
}

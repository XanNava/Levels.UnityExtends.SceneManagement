

namespace Levels.UnityExtends.SceneManagement.IoC {
    using UnityEngine.ResourceManagement.ResourceProviders;
    
    using Levels.Core;
    
    public class Broadcaster_Scene_OnSceneActivated : Notification.Broadcaster.Generic<World.Scene.Notifications.SceneActivated, SceneInstance>
    {
        public override void SetupEventHooks() {
            Levels.UnityExtends.SceneManagement.World.Scene.Management.Events.OnSceneActivated += HandleOnPerformed;
        }

        public override void BreakdownEventHooks() {
            Levels.UnityExtends.SceneManagement.World.Scene.Management.Events.OnSceneActivated -= HandleOnPerformed;
        }
    }
}

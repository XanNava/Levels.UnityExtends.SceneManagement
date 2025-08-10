

namespace Levels.UnityExtends.SceneManagement.IoC {
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceProviders;
    
    using Levels.Core;

    public class Broadcaster_Scene_OnSceneLoading : Notification.Broadcaster.Generic<World.Scene.Notifications.SceneLoading, Levels.UnityExtends.SceneManagement.World.Scene.Management.SceneLoadingProcess>
    {
        public override void SetupEventHooks() {
            Levels.UnityExtends.SceneManagement.World.Scene.Management.Events.OnSceneLoading += HandleOnPerformed;
        }

        public override void BreakdownEventHooks() {
            Levels.UnityExtends.SceneManagement.World.Scene.Management.Events.OnSceneLoading -= HandleOnPerformed;
        }
    }
}

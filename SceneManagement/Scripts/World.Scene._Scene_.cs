namespace Levels.UnityExtends.SceneManagement {
	using UnityEngine.ResourceManagement.ResourceProviders;

	public static partial class World {
		public static partial class Scene {

			public readonly struct _Scene_ {
				public readonly UnityEngine.SceneManagement.Scene? Scene;
				public readonly SceneInstance? SceneInstance;

				public _Scene_(SceneInstance sceneInstance) {
					SceneInstance = sceneInstance;
					Scene = null;
				}
				
				public _Scene_(UnityEngine.SceneManagement.Scene scene, SceneInstance? sceneInstance = null) {
					Scene = scene;
					SceneInstance = sceneInstance;
				}
			}

		}
	}
}
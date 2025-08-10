namespace Levels.UnityExtends.SceneManagement {
	
	using Addressables;
	
	using UnityEngine;
	
	[CreateAssetMenu(fileName = "IgnoreScene_SO", menuName = "Create/SceneManagement/SceneManagement.IgnoreScene_SO")]
	public class IgnoreScene_SO : ScriptableObject {
		[SerializeField]
		private AssetReferences.Scene scene;
		public AssetReferences.Scene Scene => scene;

		[SerializeField]
		private string sceneName;

		public string SceneName => string.IsNullOrEmpty(sceneName) && scene.editorAsset != null
			? scene.editorAsset.name
			: sceneName;
	}
}

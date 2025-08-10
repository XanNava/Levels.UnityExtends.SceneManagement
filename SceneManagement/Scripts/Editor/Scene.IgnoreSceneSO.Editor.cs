

namespace Levels.UnityExtends.SceneManagement.Editor {
	using UnityEditor;
	
	[CustomEditor(typeof(IgnoreScene_SO))]
	public class Scene_IgnoreSceneSO_Editor : Editor {
		public override void OnInspectorGUI() {
			base.OnInspectorGUI();

			var so = (IgnoreScene_SO)target;

			if (so.Scene != null && so.Scene.editorAsset != null) {
				string newName = so.Scene.editorAsset.name;

				if (so.SceneName != newName) {
					// Use serializedObject to update field properly
					SerializedProperty sceneNameProp = serializedObject.FindProperty("sceneName");
					sceneNameProp.stringValue = newName;
					serializedObject.ApplyModifiedProperties();

					EditorUtility.SetDirty(so);
				}
			}
		}
	}
}


#region License
// Author  : Alexander Nava 
// Contact : Alexander.Nava.Contact@Gmail.com
// License : For personal use excluding any artificail or machine learning this is licensed under MIT license.
// License : For commercial software(excluding derivative work to make libraries with the same functionality in any language) use excluding any artificail or machine learning this is licensed under MIT license.
// License : If you are a developer making money writing this software it is expected for you to donate, and thus will be given to you for any perpose other than use with Artificial Intelegence or Machine Learning this is licensed under MIT license.
// License : To any Artificial Intelegence or Machine Learning use there is no license given and is forbiden to use this for learning perposes or for anyone requesting you use these libraries, if done so will break the terms of service for this code and you will be held liable.
// License : For libraries or dirivative works that are created based on the logic, patterns, or functionality of this library must inherit all licenses here in.
// License : If you are not sure your use case falls under any of these clauses please contact me through the email above for a license.
#endregion


namespace Levels.UnityExtends.SceneManagement {
	using System.Collections;
	using System.Threading.Tasks;

	using Core;
	using Core.Results;

	using Sirenix.OdinInspector;

	using UnityEngine;
	using UnityEngine.SceneManagement;

	[AddComponentMenu("(Levels)/World/Scene/Scene.Load")]
	public class World_Scene_Load : MonoBehaviour {
		[field: SerializeField, BoxGroup("SETTINGS"), HideLabel, InlineProperty]
		public World.Scene.LoadCommand Command { get; set; }

		[field: SerializeField, BoxGroup("SETTINGS")]
		public bool AllowCancellations { get; set; }

		public World.Scene.LoadRequest LoadRequest { get; set; }
		
		#if UNITY_EDITOR
		[ShowInInspector, BoxGroup("STATUS"), ReadOnly]
		public bool HasLoadRequest => LoadRequest != null;
		[ShowInInspector, BoxGroup("STATUS"), ReadOnly]
		public bool IsActive => LoadRequest != null ? LoadRequest.IsActive : false;
		#endif

		[Button("CallLoadScene()")]
		public void CallLoadScene() {
			if (LoadRequest == null
				|| LoadRequest != null && LoadRequest.Loadmode == LoadSceneMode.Additive && LoadRequest.IsActive) {
				
				LoadRequest = null;
				StartCoroutine(CallLoadScene_Coroutine());
			}
			else {
				Debug.LogWarning("<color=yellow>[WARN]</color>[SCENE] Scene._Load.CallLoadScene() : Scene already loaded into a Session, and or is waiting activation.", gameObject);
			}
		}

		private IEnumerator CallLoadScene_Coroutine() {
			var token = AllowCancellations ? new CancelToken() : CancelToken.None;
			Task<Result<World.Scene.LoadRequest>> task = World.Scene.Management.LoadScene(Command, token);

			if (token.IsCancelled) {
				Debug.Log("<color=yellow>[WARN]</color>[CANCELLED] Scene._Load.CallLoadScene() : The load was Cancelled.", gameObject);
				yield return null;
			}
			
			while (!task.IsCompleted)
				yield return null;

			var result = task.Result;

			result.Handle(
				() => {
					Debug.Log("<color=grey>[INFO]</color>[SUCCESS] Scene._Load.CallLoadScene() : The load succeeded.", gameObject);
					
					LoadRequest = result.Value;
				},
				(e) => Debug.Log("<color=yellow>[WARN]</color>[FAILURE] Scene._Load.CallLoadScene() : The load failed. \n" + e, gameObject)
			);
		}
		
		[Button("CallActivateScene()")]
		public void CallActivateScene() {
			if (LoadRequest != null && !LoadRequest.IsActive)
				StartCoroutine(ActivateScene_Coroutine());
			else if (LoadRequest != null && LoadRequest.IsActive) {
				Debug.Log("<color=grey>[INFO]</color>[SCENE] Scene._Load.CallActivateScene() : Scene session already active. ", gameObject);
			} else {
				Debug.LogWarning("<color=yellow>[WARN]</color>[SCENE] Scene._Load.CallActivateScene() : Scene not loaded into Session. ", gameObject);
			}
		}
		
		private IEnumerator ActivateScene_Coroutine() {
			var token = AllowCancellations ? new CancelToken() : CancelToken.None;
			Task<Result> task = World.Scene.Management.ActivateScene_Async(LoadRequest, token);
			
			if (token.IsCancelled) {
				Debug.Log("<color=yellow>[WARN]</color>[CANCELLED] Scene._Load.CallActivateScene() : The load was Cancelled.", gameObject);
				yield return null;
			}

			while (!task.IsCompleted)
				yield return null;

			task.Result.Handle(
				() => {
					Debug.Log("<color=grey>[INFO]</color>[SUCCESS] Scene._Load.CallActivateScene() : Scene activated.", gameObject);
					LoadRequest.IsActive = true;
				},
				(e) => {
					Debug.LogWarning("<color=yellow>[WARN]</color>[FAILURE] Scene._Load.CallActivateScene() : Scene activation failed. \n" + e.Message);
				}
			);
		}
		
		[Button("CallUnloadScene()")]
		public void CallUnloadScene() {
			if (LoadRequest != null) {
				StartCoroutine(UnloadScene_Coroutine());
			} else {
				Debug.Log("<color=grey>[INFO]</color>[SCENE] Scene._Load.CallUnloadScene() : No Scene Session loaded.", gameObject);
			}
		}
		
		private IEnumerator UnloadScene_Coroutine() {
			if (LoadRequest != null) {
				var token = AllowCancellations ? new CancelToken() : CancelToken.None;
				
				Task task = World.Scene.Management.UnloadSceneInstance_Async(LoadRequest, token);
				
				if (token.IsCancelled) {
					Debug.Log("<color=yellow>[WARN]</color>[CANCELLED] Scene._Load.CallLoadScene() : The load was Cancelled.", gameObject);
					yield return null;
				}
				
				while (!task.IsCompleted)
					yield return null;
				
				LoadRequest = null;
				Debug.Log("<color=grey>[INFO]</color>[SCENE] Scene._Load.CallUnloadScene() : Scene unloaded.", gameObject);
			}
		}
		
		// TODO : This throughs errors on UnloadAllThatMatch if there are a few scenes loaded already.
		[Button("CallUnloadScenesMatchingCommand()")]
		public void CallUnloadScenesMatchingCommand() {
			StartCoroutine(UnloadScenesMatchingCommand_Coroutine());
		}
		
		private IEnumerator UnloadScenesMatchingCommand_Coroutine() {
			var task = World.Scene.Management.UnloadScenesMatching_Async(Command, AllowCancellations);

			while (!task.IsCompleted)
				yield return null;

			if (!LoadRequest.Instance.Scene.IsValid())
				LoadRequest = null;

			Debug.Log("<color=grey>[INFO]</color>[SCENE] Scene._Load.CallUnloadScenesMatchingCommand() : Scenes matching unloaded.", gameObject);
		}
		
		[Button("CallUnloadAllExcept()")]
		public void CallUnloadAllExcept() {
			StartCoroutine(UnloadAllExcept_Coroutine());
		}

		private IEnumerator UnloadAllExcept_Coroutine() {
			var token = AllowCancellations ? new CancelToken() : CancelToken.None;

			var sceneToKeep = LoadRequest != null && LoadRequest.Instance.Scene.IsValid()
				? LoadRequest.Instance
				: default;

			if (LoadRequest != null && !LoadRequest.IsActive && LoadRequest.Instance.Scene.IsValid()) {
				Debug.Log($"<color=grey>[INFO]</color>[SCENE] Activating keep scene: {sceneToKeep.Scene.name}", gameObject);

				var activateTask = World.Scene.Management.ActivateScene_Async(LoadRequest, token);
				while (!activateTask.IsCompleted)
					yield return null;

				var result = activateTask.Result;
				result.Handle(
					() => {
						Debug.Log($"<color=grey>[INFO]</color>[SCENE] Scene activated: {sceneToKeep.Scene.name}", gameObject);
						LoadRequest.IsActive = true;
					},
					e => Debug.LogWarning($"<color=yellow>[WARN]</color>[SCENE] Failed to activate keep scene: {e.Message}", gameObject)
				);
			}

			var unloadTask = World.Scene.Management.UnloadAllExcept_Async(sceneToKeep, AllowCancellations);

			while (!unloadTask.IsCompleted)
				yield return null;

			Debug.Log("<color=grey>[INFO]</color>[SCENE] Scene._Load.CallUnloadAllExcept() : All scenes except the current have been unloaded.", gameObject);
		}
	}
}


#region Changelog
/// <changelog>
///		<change>
///			<author></author>
///			<id></id>
///			<comment></comment>
///		</change>
/// </changelog>
#endregion
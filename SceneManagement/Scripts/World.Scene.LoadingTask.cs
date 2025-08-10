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
	#region Usings
	using System.Collections;
	using System.Threading.Tasks;

	using Core;

	using Levels.UnityExtends;

	using UnityEngine;
	using UnityEngine.ResourceManagement.ResourceProviders;
	using UnityEngine.SceneManagement;

	using static SceneManagement_Codes;
	#endregion


	public static partial class World {
		public static partial class Scene {
			public class LoadRequest {
				public SceneInstance Instance { get; }
				public LoadSceneMode Loadmode { get; }
				public bool ActivateOnLoad { get; }
				public bool IsActive { get; set;  }

				internal LoadRequest(SceneInstance instance, LoadSceneMode loadmode, bool activateOnLoad) {
					Instance = instance;
					Loadmode = loadmode;
					ActivateOnLoad = activateOnLoad;
				}

				public void ActivateScene() {
					Coroutines.Runner.Reference.StartCoroutine(this.ActivateSceneCoroutine());
				}

				public IEnumerator ActivateSceneCoroutine() {
					if (IsActive)
						yield break;

					var task = World.Scene.Management.ActivateScene_Async(this, CancelToken.None);
					yield return new WaitUntil(() => task.IsCompleted);

					if (!task.IsFaulted) {
						var result = task.Result;
						IsActive = true;

						if (result != Success()) {
							Debug.LogWarning("Scene activation returned failure result");
						}
						// add success handling here if needed
					} else {
						Debug.LogError($"Scene activation failed: {task.Exception}");
					}
				}

				public async Task<Core.Results.Result> ActivateSceneAsync(CancelToken token) {
					if (IsActive)
						return Success();

					var task = World.Scene.Management.ActivateScene_Async(this, token);
					await task;

					if (!task.IsFaulted && !token.IsCancelled) {
						Core.Results.Result result = task.Result;
						IsActive = true;
						return result;
					} else {
						Debug.LogError("Scene activation failed or was cancelled");
						return LoadingSceneOperationException();
					}
				}
			}
		}
	}
}

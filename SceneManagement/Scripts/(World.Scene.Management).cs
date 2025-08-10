namespace Levels.UnityExtends.SceneManagement {
	#region Usings
	using System;
	
	using UnityEngine;
	using UnityEngine.SceneManagement;
	
	using UnityEngine.ResourceManagement.AsyncOperations;
	using UnityEngine.ResourceManagement.ResourceProviders;
	
	using static SceneManagement_Codes;
	
	using static SceneManagement.Codes;
	
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using System.Linq;
	
	using Core.Results;
	
	using CancelToken = Core.CancelToken;
	#endregion
	
	
	public partial class World {
		public partial class Scene {
			// TODO : Add Cancel Mechanics based on notification responders, or add a second set of methods for cancelable calls.
			public static partial class Management {
				public static readonly List<IgnoreScene_SO> IgnoreScenes = new();
				
				private class LoadedSceneRecord {
					public string SceneName;
					public SceneInstance Instance;
				}
				
				private static readonly List<LoadedSceneRecord> LoadedSceneInstances = new();
				
				private static AsyncOperationHandle<SceneInstance> _loadingScene;
				private static bool _initializedIgnoreScenes;
				
				private static void CheckStatus() {
					if (!_initializedIgnoreScenes)
						LoadAllIgnoreScenes();
				}
				
				private static void LoadAllIgnoreScenes() {
					IgnoreScenes.AddRange(Resources.LoadAll<IgnoreScene_SO>("").ToList());
					_initializedIgnoreScenes = true;
				}
				
				public static async Task<Result<LoadRequest>> LoadScene(LoadCommand command, CancelToken token) {
					CheckStatus();
					
					Result<LoadRequest> result = new();
					
					if (!_loadingScene.IsDone && _loadingScene.Status == AsyncOperationStatus.None)
						return WaitingOnAsyncSceneLoad<LoadRequest>();
					
					var parameters = new LoadSceneParameters(LoadSceneMode.Additive);
					
					Events.OnSceneLoading?.Invoke(new SceneLoadingProcess(_loadingScene, token));
					
					if (token.IsCancelled) return WaitingOnAsyncSceneLoad<LoadRequest>();
					
					_loadingScene = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(
						command.Scene, parameters, false, command.Priority
					);
					
					await _loadingScene.Task;
					
					if (_loadingScene.Status != AsyncOperationStatus.Succeeded)
						return LoadingSceneOperationException<LoadRequest>();

					var instance = _loadingScene.Result;

					LoadedSceneInstances.Add(new LoadedSceneRecord {
						SceneName = instance.Scene.name,
						Instance = instance
					});

					Debug.Log($"<color=grey>[EVENT]</color> Scene Loaded: {instance.Scene.name}");
					Events.OnSceneLoaded?.Invoke(instance);
					
					LoadRequest request = new LoadRequest(instance, command.Parameters.loadSceneMode, command.ActiveOnLoad);
					result.Value = request;
					
					if (command.ActiveOnLoad) {
						await request.ActivateSceneAsync(token);
					}
					
					return result;
				}

				private static Task WrapAsyncOperation(AsyncOperation operation) {
					var tcs = new TaskCompletionSource<bool>();
					operation.completed += _ => tcs.SetResult(true);
					return tcs.Task;
				}

				public static async Task<Result> ActivateScene_Async(LoadRequest loadRequest, CancelToken token) {
					CheckStatus();

					if (!loadRequest.Instance.Scene.IsValid())
						return SceneNotValid();

					if (!loadRequest.IsActive) {
						Events.OnSceneActivating?.Invoke(new SceneActivatingProcess(loadRequest.Instance, token));
						
						if (token.IsCancelled) return RequestCanceled();
						
						var asyncOp = loadRequest.Instance.ActivateAsync();
						await WrapAsyncOperation(asyncOp);

						if (token.IsCancelled) return RequestCanceled();

						Debug.Log($"<color=grey>[EVENT]</color>[SCENE] Scene.Management.ActivateScene_Async() : Scene Activated: {loadRequest.Instance.Scene.name}");
						Events.OnSceneActivated?.Invoke(loadRequest.Instance);
					}

					if (loadRequest.Loadmode == LoadSceneMode.Single) {
						await UnloadAllExcept_Async(loadRequest.Instance, false);
					}

					return Success();
				}
				
				public static async Task UnloadAllExcept_Async(SceneInstance sceneToKeep, bool allowCancellations = true) {
					var ignoreSceneNames = GetIgnoreSceneNames();
					
					var tasks = new List<Task>();
					tasks.AddRange(UnloadTrackedScenesExcept(sceneToKeep, ignoreSceneNames, allowCancellations));
					tasks.AddRange(UnloadUntrackedScenesExcept(sceneToKeep.Scene, ignoreSceneNames, allowCancellations));
					
					await Task.WhenAll(tasks);
				}
				
				public static async Task UnloadSceneInstance_Async(LoadRequest loadRequest, CancelToken token) {
					var ignoreSceneNames = GetIgnoreSceneNames();
					
					if (loadRequest?.Instance.Scene.IsValid() == true &&
						!ignoreSceneNames.Contains(loadRequest.Instance.Scene.name)) {
						await UnloadTrackedSceneInstance_Async(loadRequest.Instance, token);
					}
				}
				
				public static async Task UnloadScenesMatching_Async(LoadCommand command, bool allowCancellations = true) {
					var ignoreSceneNames = GetIgnoreSceneNames();

					var tasks = new List<Task>();
					tasks.AddRange(UnloadTrackedScenesMatching(command.Scene.SceneName, ignoreSceneNames, allowCancellations));
					tasks.AddRange(UnloadUntrackedScenesMatching(command.Scene.SceneName, ignoreSceneNames, allowCancellations));

					await Task.WhenAll(tasks);
				}
				
				// SceneUnloading -- START
				#region SceneUloading
				private static HashSet<string> GetIgnoreSceneNames() =>
					IgnoreScenes.Select(so => so.SceneName).Where(n => !string.IsNullOrEmpty(n)).ToHashSet();
				
				private static async Task UnloadTrackedSceneInstance_Async(SceneInstance instance, CancelToken token) {
					
					Debug.Log($"<color=grey>[EVENT]</color>[SCENE] UnloadTrackedSceneInstance() : Unloading: {instance.Scene.name}");
					
					Events.OnSceneUnloading?.Invoke(new SceneUnloadingProcess(new OneOf<SceneInstance, UnityEngine.SceneManagement.Scene>(instance), token));

					if (token.IsCancelled) return;
					
					if (!instance.Scene.isLoaded) {
						var asyncOp = instance.ActivateAsync();
						await WrapAsyncOperation(asyncOp);
					}

					// TODO : This throws errors on UnloadAllThatMatch if there are a few scenes loaded already.
					string name = instance.Scene.name;
					await UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(instance).Task.ContinueWith(_ => {
						Debug.Log($"<color=grey>[EVENT]</color>[SCENE] UnloadTrackedSceneInstance() : Unloaded: {name}");
						Events.OnSceneUnloaded?.Invoke(new OneOf<SceneInstance, UnityEngine.SceneManagement.Scene>(instance));
					});

					// TODO : Check this and see why this isn't working.
					for (int i = LoadedSceneInstances.Count - 1; i >= 0; i--) {
						var record = LoadedSceneInstances[i];
						if (record.Instance.Equals(instance)) {
							LoadedSceneInstances.RemoveAt(i);
						}
					}
				}

				private static IEnumerable<Task> UnloadTrackedScenesExcept(SceneInstance sceneToKeep, HashSet<string> ignore, bool allowCancellations = true) {
					foreach (var record in LoadedSceneInstances.ToList()) {
						if (record.Instance.Scene == sceneToKeep.Scene || ignore.Contains(record.SceneName))
							continue;
						
						yield return UnloadTrackedSceneInstance_Async(record.Instance, allowCancellations ? new CancelToken() : CancelToken.None);
					}
				}

				private static IEnumerable<Task> UnloadTrackedScenesMatching(string sceneName, HashSet<string> ignore, bool allowCancellations = true) {
					foreach (var record in LoadedSceneInstances.ToList()) {
						if (record.SceneName != sceneName || ignore.Contains(record.SceneName))
							continue;

						yield return UnloadTrackedSceneInstance_Async(record.Instance, allowCancellations ? new CancelToken() : CancelToken.None);
					}
				}

				private static IEnumerable<Task> UnloadUntrackedScenesExcept(UnityEngine.SceneManagement.Scene keepScene, HashSet<string> ignore, bool allowCancelations = true) {
					for (int i = 0; i < SceneManager.sceneCount; i++) {
						var scene = SceneManager.GetSceneAt(i);
						
						// TODO : Check this to make sure it is working.
						if (!scene.isLoaded || scene == keepScene || ignore.Contains(scene.name))
							continue;

						if (LoadedSceneInstances.All(r => r.Instance.Scene != scene)) {
							yield return UnloadUntrackedScene_Async(scene, allowCancelations ? new CancelToken() : CancelToken.None);
						}
					}
				}

				private static IEnumerable<Task> UnloadUntrackedScenesMatching(string sceneName, HashSet<string> ignore, bool allowCancelations = true) {
					for (int i = 0; i < SceneManager.sceneCount; i++) {
						var scene = SceneManager.GetSceneAt(i);
						if (!scene.isLoaded || scene.name != sceneName || ignore.Contains(scene.name))
							continue;

						if (LoadedSceneInstances.All(r => r.Instance.Scene != scene)) {
							yield return UnloadUntrackedScene_Async(scene, allowCancelations ? new CancelToken() : CancelToken.None);
						}
					}
				}

				private static async Task UnloadUntrackedScene_Async(UnityEngine.SceneManagement.Scene scene, CancelToken token) {
					Debug.Log($"<color=grey>[EVENT]</color>[SCENE] DoUnloadUntrackedScene() : Unloading: {scene.name}");
					Events.OnSceneUnloading?.Invoke(new SceneUnloadingProcess(new OneOf<SceneInstance, UnityEngine.SceneManagement.Scene>(scene), token));
					
					if (token.IsCancelled) return;
					
					await WrapAsyncOperation(SceneManager.UnloadSceneAsync(scene)).ContinueWith(
						_ => {
						Debug.Log($"<color=grey>[EVENT]</color>[SCENE] DoUnloadUntrackedScene() : Unloaded: {scene.name}");
						Events.OnSceneUnloaded?.Invoke(new OneOf<SceneInstance, UnityEngine.SceneManagement.Scene>(scene));
					});
				}
				#endregion
				// SceneUnloading -- END
				
				public static class Events {
					public static Action<SceneLoadingProcess> OnSceneLoading;
					public static Action<SceneInstance> OnSceneLoaded;
					
					public static Action<SceneActivatingProcess> OnSceneActivating;
					public static Action<SceneInstance> OnSceneActivated;
					
					public static Action<SceneUnloadingProcess> OnSceneUnloading;
					public static Action<OneOf<SceneInstance, UnityEngine.SceneManagement.Scene>> OnSceneUnloaded;
				}
				
				public struct SceneLoadingProcess {
					public AsyncOperationHandle<SceneInstance> Handle;
					public CancelToken Token;

					public SceneLoadingProcess(AsyncOperationHandle<SceneInstance> handle, CancelToken token) {
						Handle = handle;
						Token = token;
					}
				}

				public struct SceneActivatingProcess {
					public SceneInstance Instance;
					public CancelToken Token;

					public SceneActivatingProcess(SceneInstance instance, CancelToken token) {
						Instance = instance;
						Token = token;
					}
				}

				public struct SceneUnloadingProcess {
					public OneOf<SceneInstance, UnityEngine.SceneManagement.Scene> Case;
					public CancelToken Token;

					public SceneUnloadingProcess(OneOf<SceneInstance, UnityEngine.SceneManagement.Scene> @case, CancelToken token) {
						Case = @case;
						Token = token;
					}
				}
			}
		}
	}

	public static class SceneManagement {
		public static class Codes {
			public class WaitingOnAsyncSceneLoad : Failure { }
			public class RequestCanceled : Failure { }
			public class SceneNotValid : Failure { }
			public class InvalidActivationTaskStatus : Failure { }
			public class LoadingSceneOperationException : Failure { }
		}
	}

	public static class SceneManagement_Codes {
		public static Result WaitingOnAsyncSceneLoad() {
			return new Result(new WaitingOnAsyncSceneLoad());
		}
		
		public static Result RequestCanceled() {
			return new Result(new RequestCanceled());
		}
		
		public static Result InvalidActivationTaskStatus() {
			return new Result(new InvalidActivationTaskStatus());
		}
		public static Result SceneNotValid() {
			return new Result(new InvalidActivationTaskStatus());
		}

		public static Result LoadingSceneOperationException() {
			return new Result(new LoadingSceneOperationException());
		}

		public static Result Success() {
			return new Result(new Success());
		}

		public static Result<T> WaitingOnAsyncSceneLoad<T>() {
			return new Result<T>(new WaitingOnAsyncSceneLoad());
		}
		public static Result<T> LoadingSceneOperationException<T>() {
			return new Result<T>(new LoadingSceneOperationException());
		}
		public static Result<T> RequestCanceled<T>() {
			return new Result<T>(new LoadingSceneOperationException());
		}
		public static Result<T> Success<T>() {
			return new Result<T>(new Success());
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
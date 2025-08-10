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


namespace Levels.UnityExtends.Addressables {
	using System;
	using UnityEngine;
	using UnityEngine.AddressableAssets;

	public partial class AssetReferences {
		[Serializable]
		public class Scene : AssetReference {

			[SerializeField]
			private string _sceneName = string.Empty;

			public string SceneName => _sceneName;

			public Scene(string guid) : base(guid) { }

			public override bool ValidateAsset(string path) {
				var check = path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
				if (!check) {
					Debug.Log("<color=red>[ERROR]</color>[VALIDATE] AssetReferences.Scene.ValidateAsset() : Invalid Asset Reference Type. Please use only '.unity' scenes.");
				}

				#if UNITY_EDITOR
				if (check) {
					_sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
				}
				#endif

				return check;
			}

			#if UNITY_EDITOR
			public override bool ValidateAsset(UnityEngine.Object obj) {
				string path = UnityEditor.AssetDatabase.GetAssetPath(obj);
				return ValidateAsset(path);
			}
			#endif
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
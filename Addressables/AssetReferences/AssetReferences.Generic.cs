
namespace Levels.UnityExtends.Addressables  {
	using System;

	using UnityEngine;
	using UnityEngine.AddressableAssets;

	public partial class AssetReferences {
		[Serializable]
		public abstract class generic : AssetReference {
			[field: SerializeField]
			public string extension {  get; set; }

			public generic(string guid) : base(guid) {
				this.extension = extension;
			}

			public override bool ValidateAsset(string path) {
				var check = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
				if (!check) {
					Debug.Log("<color=red>[ERROR]</color>[VALIDATE] AssetReferences.Json.ValidateAsset() : Invalid Asset Reference Type please use to of '.json' only.");
				}
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




namespace Levels.UnityExtends.Addressables {
	using System;
	
	public partial class AssetReferences {
		[Serializable]
		public class Json : Addressables.AssetReferences.generic {
			public Json(string guid) : base(guid) {
				extension = ".json";
			}
		}
	}
}

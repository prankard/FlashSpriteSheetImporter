using UnityEngine;
using System.Collections;

namespace Prankard.FlashSpriteSheetImporter
{
	public interface ISpriteSheetParser 
	{
		bool ParseAsset(Texture2D asset, TextAsset textAsset);
	}
}
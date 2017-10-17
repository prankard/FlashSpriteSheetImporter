using UnityEngine;
using System.Collections;

namespace Prankard.FlashSpriteSheetImporter
{
	public interface ISpriteSheetParser 
	{
		bool ParseAsset(Texture2D asset, TextAsset textAsset, Vector2 pivot);
		/// <summary>
		/// Gets the file extension without dot prefix
		/// </summary>
		/// <value>The file extension eg, "xml"</value>
		string FileExtension { get; }
	}
}
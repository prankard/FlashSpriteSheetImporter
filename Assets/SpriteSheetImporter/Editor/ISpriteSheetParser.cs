using UnityEngine;
using System.Collections;

namespace Prankard.FlashSpriteSheetImporter
{
	public interface ISpriteSheetParser 
	{
		//Modified line (added parameters):
		bool ParseAsset(Texture2D asset, TextAsset textAsset, Vector2 pivot, bool forcePivotOverwrite);
		/// <summary>
		/// Gets the file extension without dot prefix
		/// </summary>
		/// <value>The file extension eg, "xml"</value>
		string FileExtension { get; }
	}
}
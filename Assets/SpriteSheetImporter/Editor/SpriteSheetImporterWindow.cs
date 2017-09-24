using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Reflection;

namespace Prankard.FlashSpriteSheetImporter
{
	public class SpriteSheetImporterWindow : EditorWindow
	{
		private static ISpriteSheetParser[] spriteParsers = new ISpriteSheetParser[]{new FlashSpriteSheetParser()};

		[MenuItem ("Window/Sprite Sheet Importer")]
		static void Init () {
			SpriteSheetImporterWindow window = (SpriteSheetImporterWindow)EditorWindow.GetWindow (typeof (SpriteSheetImporterWindow));
			window.titleContent = new GUIContent("Sprite Sheet Importer", "Sprite Sheet Importer");
			window.Show();
		}

		private Texture2D spriteSheet;
		private TextAsset textAsset;
		
		void OnGUI () 
		{
			GUILayout.Label ("Texture", EditorStyles.boldLabel);
			Texture2D newSpriteSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", spriteSheet, typeof(Texture2D), false);
			if (newSpriteSheet != null)
			{
				if (spriteSheet != newSpriteSheet)
				{
					// Look for text asset
					string assetPath = AssetDatabase.GetAssetPath(newSpriteSheet);
					string xmlPath = Path.GetDirectoryName(assetPath) + "/" + Path.GetFileNameWithoutExtension(assetPath) + ".xml";
					TextAsset searchTextAsset = AssetDatabase.LoadAssetAtPath(xmlPath,typeof(TextAsset)) as TextAsset;
					if (searchTextAsset != null)
					{
						textAsset = searchTextAsset;
					}
				}
				spriteSheet = newSpriteSheet;
			}
			else
				spriteSheet = null;

			GUILayout.Label ("Sprites Information", EditorStyles.boldLabel);
			textAsset = (TextAsset)EditorGUILayout.ObjectField("Sprite Sheet XML", textAsset, typeof(TextAsset), false);
			
			GUILayout.Space(10);
			if (textAsset != null && spriteSheet != null)
			{
				if (GUILayout.Button("Import Sprites"))
				{
					Vector2 size = GetOriginalSize(newSpriteSheet);
					if (size.x  != spriteSheet.width && size.y != spriteSheet.height)
					{
						Debug.LogWarning("Cannot convert sprite sheet when it's not it's original size. It's original size is '" + size.x +"x" + size.y+"' and build size is '" + spriteSheet.width + "x" + spriteSheet.height + "'. You can change the texture size to it's original size, import sprites and then change the texture size back.");
						return;
					}

					foreach (ISpriteSheetParser parser in spriteParsers)
					{
						if (parser.ParseAsset(spriteSheet, textAsset))
						{
							Debug.Log("Imported Sprites");
							return;
						}
					}
					Debug.LogError("Failed To Parse Asset");
				}
			}
			else
			{
				GUILayout.Label ("Cannot Import", EditorStyles.boldLabel);
				GUILayout.Label ("Please select a sprite sheet and text asset to import sprite sheet", EditorStyles.textArea);
			}
		}
		
		public static Vector2 GetOriginalSize(Texture2D texture)
		{
			string assetPath = AssetDatabase.GetAssetPath(texture);
			TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
			if (importer == null)
				return new Vector2(texture.width, texture.height);
			
			object[] array = new object[]{0,0};
			MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
			mi.Invoke(importer, array);
			
			return new Vector2((int)array[0], (int)array[1]);
		}
	}
}
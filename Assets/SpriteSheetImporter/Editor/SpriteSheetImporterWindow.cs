using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Prankard.FlashSpriteSheetImporter
{
	public class SpriteSheetImporterWindow : EditorWindow
	{
		private static Dictionary<SpriteDataFormat, ISpriteSheetParser> spriteParsers = new Dictionary<SpriteDataFormat, ISpriteSheetParser> ()
		{
			{ SpriteDataFormat.StarlingOrSparrowV2, new StarlingParser() }
		};

		[MenuItem ("Window/Sprite Sheet Importer")]
		static void Init () {
			SpriteSheetImporterWindow window = (SpriteSheetImporterWindow)EditorWindow.GetWindow (typeof (SpriteSheetImporterWindow));
			window.titleContent = new GUIContent("Sprite Sheet Importer", "Sprite Sheet Importer");
			window.Show();
		}

		private Vector2 customPivot = Vector2.zero;

		private Texture2D spriteSheet;
		private TextAsset textAsset;
		private SpriteDataFormat dataFormat = SpriteDataFormat.StarlingOrSparrowV2;
		private SpriteAlignment spriteAlignment = SpriteAlignment.TopLeft;
        private bool forcePivotOverwrite = false;
        private bool generateSpriteSheet = false;
        private bool generateGameObject = false;
        private float fps = 24.0f;

        private string errorMessage = null;

        void OnGUI()
        {
            GUILayout.Label("Texture", EditorStyles.boldLabel);
            Texture2D newSpriteSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", spriteSheet, typeof(Texture2D), false);
            if (newSpriteSheet != null)
            {
                if (spriteSheet != newSpriteSheet)
                {
                    // Look for text asset
                    string assetPath = AssetDatabase.GetAssetPath(newSpriteSheet);

                    foreach (ISpriteSheetParser parser in spriteParsers.Values)
                    {
                        var dataAssetPath = Path.GetDirectoryName(assetPath) + "/" + Path.GetFileNameWithoutExtension(assetPath) + "." + parser.FileExtension;
                        TextAsset searchTextAsset = AssetDatabase.LoadAssetAtPath(dataAssetPath, typeof(TextAsset)) as TextAsset;
                        if (searchTextAsset != null)
                        {
                            textAsset = searchTextAsset;
                            break;
                        }
                    }
                }
                spriteSheet = newSpriteSheet;
            }
            else
                spriteSheet = null;

            GUILayout.Label("Sprites Information", EditorStyles.boldLabel);
            textAsset = (TextAsset)EditorGUILayout.ObjectField("Sprite Sheet XML", textAsset, typeof(TextAsset), false);
            dataFormat = (SpriteDataFormat)EditorGUILayout.EnumPopup("Data Format", dataFormat);

            GUILayout.Label("Import Options", EditorStyles.boldLabel);
            forcePivotOverwrite = EditorGUILayout.Toggle("Force Overwrite Pivot", forcePivotOverwrite);

            if (forcePivotOverwrite)
            {
                spriteAlignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Sprite Pivot", spriteAlignment);
                if (spriteAlignment == SpriteAlignment.Custom) {
                    customPivot = EditorGUILayout.Vector2Field("Custom Pivot", customPivot);
                }
            }

            GUILayout.Label("Animation Options", EditorStyles.boldLabel);
            generateSpriteSheet = EditorGUILayout.Toggle("Generate Animation", generateSpriteSheet);
            if (generateSpriteSheet)
            {
                fps = EditorGUILayout.FloatField("Frames Per Second", fps);
                generateGameObject = EditorGUILayout.Toggle("Create GameObject", generateGameObject);
            }

            GUILayout.Space(10);
            if (textAsset != null && spriteSheet != null)
            {
                if (GUILayout.Button("Import Sprites"))
                {
                    errorMessage = null;

                    Vector2 size = GetOriginalSize(newSpriteSheet);
                    if (size.x != spriteSheet.width && size.y != spriteSheet.height)
                    {
                        errorMessage = "Cannot convert sprite sheet when it's not it's original size. It's original size is '" + size.x + "x" + size.y + "' and build size is '" + spriteSheet.width + "x" + spriteSheet.height + "'. You can change the texture size to it's original size, import sprites and then change the texture size back.";
                        Debug.LogWarning(errorMessage);
                        //Debug.LogWarning("Cannot convert sprite sheet when it's not it's original size. It's original size is '" + size.x +"x" + size.y+"' and build size is '" + spriteSheet.width + "x" + spriteSheet.height + "'. You can change the texture size to it's original size, import sprites and then change the texture size back.");
                        return;
                    }

                    if (spriteParsers[dataFormat].ParseAsset(spriteSheet, textAsset, forcePivotOverwrite ? PivotValue : new Vector2(0f, 1.0f), forcePivotOverwrite))
                    {
                        Debug.Log("Imported Sprites");
                        if (generateSpriteSheet)
                        {
                            AnimationCreator.GenerateAnimation(spriteSheet, fps, generateGameObject);
                        }

                        return;
                    }

                    Debug.LogError("Failed To Parse Asset");
                }
            }
            else
            {
                GUILayout.Label("Cannot Import", EditorStyles.boldLabel);
                GUILayout.Label("Please select a sprite sheet and text asset to import sprite sheet", EditorStyles.helpBox);
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox(errorMessage, MessageType.Warning);
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

		public Vector2 PivotValue
		{
			get
			{
				switch (spriteAlignment)
				{
				case SpriteAlignment.TopLeft:
					return new Vector2 (0f, 1f);
				case SpriteAlignment.TopCenter:
					return new Vector2 (0.5f, 1f);
				case SpriteAlignment.TopRight:
					return new Vector2 (1f, 1f);
				case SpriteAlignment.LeftCenter:
					return new Vector2 (0f, 0.5f);
				case SpriteAlignment.Center:
					return new Vector2 (0.5f, 0.5f);
				case SpriteAlignment.RightCenter:
					return new Vector2 (1f, 0.5f);
				case SpriteAlignment.BottomLeft:
					return new Vector2 (0f, 0f);
				case SpriteAlignment.BottomCenter:
					return new Vector2 (0.5f, 0f);
				case SpriteAlignment.BottomRight:
					return new Vector2 (1f, 0f);
				case SpriteAlignment.Custom:
					return customPivot;
				default:
					throw new System.NotImplementedException ("I don't know the sprite alignment: " + spriteAlignment.ToString ());
				}
			}
		}
	}
}
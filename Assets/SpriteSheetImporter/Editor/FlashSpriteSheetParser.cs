using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;
using System.Xml;
using System.Collections.Generic;
using System.Reflection;

namespace Prankard.FlashSpriteSheetImporter
{
	public class FlashSpriteSheetParser : ISpriteSheetParser 
	{
		public bool ParseAsset (Texture2D asset, TextAsset textAsset)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(textAsset.text);
			
			XmlNodeList subTextures = doc.SelectNodes("//SubTexture");
			List<SpriteMetaData> spriteSheet = new List<SpriteMetaData>();
			
			foreach (XmlNode node in subTextures)
			{
				string name = GetAttribute(node, "name");
				float x = float.Parse(GetAttribute(node, "x", "0"));
				float y = float.Parse(GetAttribute(node, "y", "0"));
				float frameX = float.Parse(GetAttribute(node, "frameX", "0"));
				float frameY = float.Parse(GetAttribute(node, "frameY", "0"));
				float width = float.Parse(GetAttribute(node, "width", "0"));
				float height = float.Parse(GetAttribute(node, "height", "0"));
				
				if (width != 0 && height != 0)
				{
					SpriteMetaData smd = new SpriteMetaData();
					smd.name = name;
					smd.rect = new Rect(x, asset.height - y - height, width, height);

					// Fix from Mikhail Pechaneu, thanks!
					smd.pivot = new Vector2(frameX / width, -(frameY - height) / height); // pivot is percent value, not pixels
					smd.alignment = 9; // We should use custom alignment, otherwise it will use Center alignment https://docs.unity3d.com/ScriptReference/SpriteMetaData-alignment.html

					spriteSheet.Add(smd);
				}
			}
			
			if (spriteSheet.Count != 0)
			{
				string assetPath = AssetDatabase.GetAssetPath(asset);
				TextureImporter importer = TextureImporter.GetAtPath(assetPath) as TextureImporter;
				importer.spritesheet = spriteSheet.ToArray();
				importer.textureType = TextureImporterType.Sprite;
				importer.spriteImportMode = SpriteImportMode.Multiple;
				AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
				return true;
			}
			else
			{
	//			Debug.Log("No sprites found in: " + AssetDatabase.GetAssetPath(textAsset));
			}
			return false;
		}
		
		private static string GetAttribute(XmlNode node, string name, string defaultValue = "")
		{
			XmlNode attribute = node.Attributes.GetNamedItem(name);
			if (attribute == null)
				return defaultValue;
			return attribute.Value;
		}
	}
}
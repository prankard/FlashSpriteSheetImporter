using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;
using System.Xml;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Sparrow v2 parser
/// Parses an XML Sparrow 2d file into Unity Sprites
/// For reference for the sparrow2d file format, see here:
/// https://github.com/Gamua/Starling-Framework/blob/master/starling/src/starling/textures/TextureAtlas.as
/// </summary>

namespace Prankard.FlashSpriteSheetImporter
{
	public class SparrowV2Parser : ISpriteSheetParser 
	{
		public string FileExtension
		{
			get
			{
				return "xml";
			}
		}

		public bool ParseAsset (Texture2D asset, TextAsset textAsset, Vector2 pivot)
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
				// We can't handle 'trim' option in Unity3d yet as we can't add extra empty space to sprite border
				/*
				float frameX = float.Parse(GetAttribute(node, "frameX", "0"));
				float frameY = float.Parse(GetAttribute(node, "frameY", "0"));
				float frameWidth = float.Parse(GetAttribute(node, "frameWidth", "0"));
				float frameHeight = float.Parse(GetAttribute(node, "frameHeight", "0"));
				*/
				float width = float.Parse(GetAttribute(node, "width", "0"));
				float height = float.Parse(GetAttribute(node, "height", "0"));
				
				if (width != 0 && height != 0)
				{
					SpriteMetaData smd = new SpriteMetaData();
					smd.name = name;
					smd.rect = new Rect(x, asset.height - y - height, width, height);

					smd.pivot = pivot;
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
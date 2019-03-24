using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;
using System.Xml;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Linq;

/// <summary>
/// Starling parser
/// Parses an XML Starling file into Unity Sprites (or Sparrow 2d)
/// For reference for the sparrow2d file format, see here:
/// https://github.com/Gamua/Starling-Framework/blob/master/starling/src/starling/textures/TextureAtlas.as
/// </summary>

namespace Prankard.FlashSpriteSheetImporter
{
	public class StarlingParser : ISpriteSheetParser
    {
		public string FileExtension
		{
			get
			{
				return "xml";
			}
		}

		//Modified line (added parameters):
		public bool ParseAsset (Texture2D asset, TextAsset textAsset, Vector2 inputPivot, bool forcePivotOverwrite)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(textAsset.text);
			
			XmlNodeList subTextures = doc.SelectNodes("//SubTexture");
			List<SpriteMetaData> spriteSheet = new List<SpriteMetaData>();

			foreach (XmlNode node in subTextures)
			{
				string name = GetAttribute(node, "name");

				float x = GetFloatAttribute(node, "x"); 
				float y = GetFloatAttribute(node, "y");
				float width = GetFloatAttribute(node, "width");
				float height = GetFloatAttribute(node, "height");


                // Pivot (starling only, effects next sprite pivots)
                if (!forcePivotOverwrite && (HasAttribute(node, "pivotX") || HasAttribute(node, "pivotY")))
                {
                    if (width != 0)
                        inputPivot.x = GetFloatAttribute(node, "pivotX") / width;
                    if (height != 0)
                        inputPivot.y = 1 - (GetFloatAttribute(node, "pivotY") / height);
                }

                Vector2 spritePivot = inputPivot;

                // Adjust pivot for trim whitespace
                if (width != 0 && HasAttribute(node, "frameX"))
                {
                    float frameX = GetFloatAttribute(node, "frameX");
                    float frameWidth = GetFloatAttribute(node, "frameWidth");

                    spritePivot.x = (spritePivot.x * frameWidth + frameX) / width;
                }
                if (height != 0 && HasAttribute(node, "frameY"))
                {
                    float frameY = GetFloatAttribute(node, "frameY");
                    float frameHeight = GetFloatAttribute(node, "frameHeight");

                    spritePivot.y = 1 - (frameHeight - (spritePivot.y * frameHeight) + frameY) / height;
                }

                // Make Sprite
				SpriteMetaData smd = new SpriteMetaData();
				smd.name = name;
				smd.rect = new Rect(x, asset.height - y - height, width, height);
				smd.pivot = spritePivot;
				smd.alignment = 9; // Custom Sprite alignment (not center)

				spriteSheet.Add(smd);
			}
			
			if (spriteSheet.Count != 0)
			{
				string assetPath = AssetDatabase.GetAssetPath(asset);
				TextureImporter importer = TextureImporter.GetAtPath(assetPath) as TextureImporter;
				importer.spritesheet = spriteSheet.ToArray();
				importer.textureType = TextureImporterType.Sprite;
				importer.spriteImportMode = SpriteImportMode.Multiple;
				AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return true;
			}
			else
			{
				Debug.Log("No sprites found in: " + AssetDatabase.GetAssetPath(textAsset));
			}
			return false;
		}

        private static float GetFloatAttribute(XmlNode node, string name, float defaultValue = 0)
        {
            XmlNode attribute = node.Attributes.GetNamedItem(name);
            if (attribute == null)
                return defaultValue;

            return float.Parse(attribute.Value, CultureInfo.InvariantCulture);
        }
		
		private static string GetAttribute(XmlNode node, string name, string defaultValue="")
		{
			XmlNode attribute = node.Attributes.GetNamedItem(name);
			if (attribute == null)
				return defaultValue;
			return attribute.Value;
		}

        private static bool HasAttribute(XmlNode node, string name)
        {
            return node.Attributes.GetNamedItem(name) != null;
        }
	}
}
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

            //bool pivotSet = false; //not used anymore
            Vector2 pivotPixels;

			foreach (XmlNode node in subTextures)
			{
				string name = GetAttribute(node, "name");

				float x = GetFloatAttribute(node, "x"); 
				float y = GetFloatAttribute(node, "y");
				float width = GetFloatAttribute(node, "width");
				float height = GetFloatAttribute(node, "height");
                pivotPixels.x = inputPivot.x * width;
                pivotPixels.y = inputPivot.y * height;

                //Debug.Log(width);
                // Pivot (starling only, effects next sprite pivots)
                if (!forcePivotOverwrite && (HasAttribute(node, "pivotX") || HasAttribute(node, "pivotY")))
                {
                    //Debug.Log(GetFloatAttribute(node, "pivotX"));
                    pivotPixels.x = GetFloatAttribute(node, "pivotX");
                    pivotPixels.y = GetFloatAttribute(node, "pivotY");
                    float frameWidth = GetFloatAttribute(node, "frameWidth");
                    float frameHeight = GetFloatAttribute(node, "frameHeight");

                    if (frameWidth != 0)
                        inputPivot.x = pivotPixels.x / frameWidth;
                    else if (width != 0)
                        inputPivot.x = pivotPixels.x / width;

                    if (frameHeight != 0)
                    {
                        inputPivot.y = 1 - pivotPixels.y / frameHeight;
                        pivotPixels.y = frameHeight - inputPivot.y; // flip pivot
                    }
                    else if (height != 0)
                    {
                        inputPivot.y = 1 - pivotPixels.y / height;
                        pivotPixels.y = height - pivotPixels.y; // flip pivot
                    }

                    //pivotPixels.y = GetFloatAttribute(node, "pivotY");

                    //Debug.Log(inputPivot.x +"," + inputPivot.y);

                    /*
                    if (width != 0)
                        inputPivot.x = GetFloatAttribute(node, "pivotX") / width;
                    if (height != 0)
                        inputPivot.y = 1 - (GetFloatAttribute(node, "pivotY") / height);
                        */



                    //Debug.Log(inputPivot.x);
                }

                //Vector2 spritePivot = inputPivot;
                // Check for zero divide

                Vector2 spritePivot = new Vector2(pivotPixels.x / width, pivotPixels.y / height);

                // Adjust pivot for trim whitespace
                if (width != 0 && HasAttribute(node, "frameX"))
                {
                    float frameX = GetFloatAttribute(node, "frameX");
                    float frameWidth = GetFloatAttribute(node, "frameWidth");
                    pivotPixels.x = inputPivot.x * frameWidth;

                    //pivotPixels.x = frameWidth * inputPivot.x;

                    spritePivot.x = (pivotPixels.x + frameX) / width;
                    //Debug.Log(spritePivot.x + " = (" + pivotPixels.x + " + " + frameX + ") / " + width);

                    //spritePivot.x = (spritePivot.x * frameWidth + frameX) / width;
                }
                if (height != 0 && HasAttribute(node, "frameY"))
                {
                    float frameY = GetFloatAttribute(node, "frameY");
                    float frameHeight = GetFloatAttribute(node, "frameHeight");
                    pivotPixels.y = inputPivot.y * frameHeight;
                    //pivotPixels.y = frameHeight * inputPivot.y;

                    //spritePivot.y = ((pivotPixels.y) + frameY) / height;
					spritePivot.y = ((height + pivotPixels.y - frameY) / height) - (frameHeight / height); //BUGFIX on Y pivot
                    //Debug.Log(name);
                    //Debug.Log(spritePivot.y + " = (" + pivotPixels.y + ") / " + height);
                    //spritePivot.y = 1 - (frameHeight - (spritePivot.y * frameHeight) + frameY) / height;
                }
                else
                {
                    //Debug.Log(name);
                    //Debug.Log(spritePivot.y + " = (" + pivotPixels.y + ") / " + height);
                }

				//Added security mechanism:
                if(float.IsNaN(spritePivot.x) || float.IsNaN(spritePivot.y))
                { 
                    //spritePivot is invalid, probably because the sprite dimensions are equal to 0
                    spritePivot = Vector2.zero; //pivot is set to zero, to prevent animation errors
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
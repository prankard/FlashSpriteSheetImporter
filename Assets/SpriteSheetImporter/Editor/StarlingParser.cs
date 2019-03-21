using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;
using System.Xml;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;

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
		public bool ParseAsset (Texture2D asset, TextAsset textAsset, Vector2 pivot, bool useSpriteAutoAlignMode, bool useXMLPivot)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(textAsset.text);
			
			XmlNodeList subTextures = doc.SelectNodes("//SubTexture");
			List<SpriteMetaData> spriteSheet = new List<SpriteMetaData>();

			//Modified block begining
			//Allows the use of Starling pivot data:
			bool isFirstNode = true;
			Vector2 pxPivot = Vector2.zero;
			float frameHeight = 0;
			float frameWidth = 0;
			//End of modified block

			foreach (XmlNode node in subTextures)
			{
				string name = GetAttribute(node, "name");
				Debug.Log("Spritesheet import :"+name);
				//Modified block begining
				// CultureInfo.InvariantCulture is used to bypass Regional Settings which may be configured 
				// on some computers to use comma instead of period (e.g. French, German)
				float x = float.Parse(GetAttribute(node, "x", "0"), CultureInfo.InvariantCulture); 
				float y = float.Parse(GetAttribute(node, "y", "0"), CultureInfo.InvariantCulture);
				float width = float.Parse(GetAttribute(node, "width", "0"), CultureInfo.InvariantCulture);
				float height = float.Parse(GetAttribute(node, "height", "0"), CultureInfo.InvariantCulture);
				//End of modified block

				//Modified block begining
				// To consider the relative positionning of each frame of the animation:
				float frameX = float.Parse(GetAttribute(node, "frameX", "0"), CultureInfo.InvariantCulture);
				float frameY = float.Parse(GetAttribute(node, "frameY", "0"), CultureInfo.InvariantCulture);

				if(isFirstNode){
					isFirstNode=false;

					frameWidth = float.Parse(GetAttribute(node, "frameWidth", "0"), CultureInfo.InvariantCulture);
					frameHeight = float.Parse(GetAttribute(node, "frameHeight", "0"), CultureInfo.InvariantCulture);

					if(!useXMLPivot){
						//A pivot has been selected by the user in the menu,
						//It will be converted to consider the MovieClip dimensions: 
						pxPivot.x = frameWidth  * pivot.x;
						pxPivot.y = frameHeight * (pivot.y - 1);
						//Debug.Log("Manual pivot mode. pivot="+pivot+" pxPivot="+pxPivot+".");
					}
				}	
				//End of Modified block

				//if (width != 0 && height != 0) //Condition removed because it was causing a problem when the first frame of a clip was empty (which may be usefull)
				//{                              //However, width or height equal to zero are treated bellow to prevent math errors
					SpriteMetaData smd = new SpriteMetaData();
					smd.name = name;
					smd.rect = new Rect(x, asset.height - y - height, width, height);

					//Modified block begining
					smd.pivot = pivot; //this will be the default value

					//Debug.Log("Pivot frame 1="+pivot.ToString("F3")+" pivot mc="+mcPivot.ToString("F3")); //F3 spécifie 3 décimales

					if(useXMLPivot){
						//the pivot in the XML data should be used
						string txtPivotX = GetAttribute(node, "pivotX"); //default value will be an empty string
						string txtPivotY = GetAttribute(node, "pivotY");
						if(txtPivotX != "" && txtPivotY != ""){
							//both pivot info were found

							//Debug.Log("txtPivotX="+txtPivotX+" txtPivotY"+txtPivotY);
							pxPivot.x = float.Parse(txtPivotX, CultureInfo.InvariantCulture);
							pxPivot.y = float.Parse(txtPivotY, CultureInfo.InvariantCulture);
							pxPivot.y *= -1; //Y value is inverted
							//Debug.Log("XMLPivot detected for the sprite «"+name+"». pxPivot="+pxPivot+".");
						}
					}

					if(useSpriteAutoAlignMode){
						//the frame should be positionned relatively to its position inside the original MovieClip
						Vector2 imgPos = Vector2.zero;
						if (width != 0){
							imgPos.x = (pxPivot.x + frameX) / width;
						} else { //this default value prevents divide by zero errors:
							imgPos.x = 0;
						}
					
						if (height != 0){
							imgPos.y = ((height + pxPivot.y - frameY) / height);
						} else { //this default value prevents divide by zero errors:
							imgPos.y = 0;
						}


						smd.pivot = imgPos; //the position of this frame will be used as a pivot 
					}
					//End of modification block
					
					smd.alignment = 9; // We should use custom alignment, otherwise it will use Center alignment https://docs.unity3d.com/ScriptReference/SpriteMetaData-alignment.html

					spriteSheet.Add(smd);
				//}
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
		
		private static string GetAttribute(XmlNode node, string name, string defaultValue="")
		{
			XmlNode attribute = node.Attributes.GetNamedItem(name);
			if (attribute == null)
				return defaultValue;
			return attribute.Value;
		}

	}
}
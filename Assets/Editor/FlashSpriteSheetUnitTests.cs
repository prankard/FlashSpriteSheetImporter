using NUnit.Framework;
using Prankard.FlashSpriteSheetImporter;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
//using UnityEngine.Assertions;
using UnityEngine.TestTools;

public class FlashSpriteSheetUnitTests
{
    private ISpriteSheetParser parser = new StarlingParser();

    [TestCase]
    public void CheckTextureImporterExists()
    {
        // This unnamed method helps us with width and heigh pre-import, if this doesn't exist anymore. There will probably be a new method/workaround
        MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(mi);
    }

    private static string TestFolder
    {
        get
        {
            return "Assets/Tests";
        }
    }

    [TestCase]
    public void TestInvalidDimensions()
    {
        var path = Path.Combine(TestFolder, "SpriteSheetWithInvalidDimensions.png");
        Debug.Log(path);
        TestGenerateSpriteSheet(path);
    }

    [TearDown]
    public void TearDown()
    {
        DeleteGeneratedAnimations();
    }
    
    public void DeleteGeneratedAnimations()
    {
        var generatedPath = Application.dataPath + "/../" + TestFolder + "/Animations-Generated";
        Debug.Log(generatedPath);
        if (Directory.Exists(generatedPath))
        {
            Debug.Log("It exists");
            Directory.Delete(generatedPath, true);
        }
    }

    public void TestGenerateSpriteSheet(string assetPath)
    {
        Texture2D texture = (Texture2D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
        var dataAssetPath = Path.GetDirectoryName(assetPath) + "/" + Path.GetFileNameWithoutExtension(assetPath) + "." + parser.FileExtension;
        TextAsset textAsset = AssetDatabase.LoadAssetAtPath(dataAssetPath, typeof(TextAsset)) as TextAsset;
        if (!parser.ParseAsset(texture, textAsset, new Vector2(0.5f, 0.5f), false))
        {
            Assert.Fail("Could not parse texture asset");
        }
        Assert.Pass();
    }
}

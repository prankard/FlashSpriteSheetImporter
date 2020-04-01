using NUnit.Framework;
using Prankard.FlashSpriteSheetImporter;
using System;
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
        //LogAssert.ignoreFailingMessages = true;
        LogAssert.Expect(LogType.Error, "Invalid dimensions detected for sprite 'Clip 10028' (width=-2.147483E+08, height=-2.147483E+08). Import has to be aborted. Please check the XML file content.");
        var path = Path.Combine(TestFolder, "SpriteSheetWithInvalidDimensions.png");
        TestGenerateSpriteSheet(path, true);
    }

    [TearDown]
    public void TearDown()
    {
        //DeleteGeneratedAnimations(); // this function fails currently and crashes unity as we're removing files, would like to test animations and clear the generated files
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

    public void TestGenerateSpriteSheet(string assetPath, bool shouldFail = false)
    {
        Texture2D texture = (Texture2D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
        Debug.Log(texture);
        var dataAssetPath = Path.GetDirectoryName(assetPath) + "/" + Path.GetFileNameWithoutExtension(assetPath) + "." + parser.FileExtension;
        Debug.Log(dataAssetPath);
        TextAsset textAsset = AssetDatabase.LoadAssetAtPath(dataAssetPath, typeof(TextAsset)) as TextAsset;
        if (!parser.ParseAsset(texture, textAsset, new Vector2(0.5f, 0.5f), false))
        {
            if (!shouldFail)
                Assert.Fail("Could not parse texture asset");
            else
                Assert.Pass();
        }
        if (!shouldFail)
            Assert.Pass();
        else
            Assert.Fail();
    }
}

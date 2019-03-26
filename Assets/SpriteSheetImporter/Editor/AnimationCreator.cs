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
/// Animation Creator, a helpful static class for generating animation based on tilesheets
/// </summary>

namespace Prankard.FlashSpriteSheetImporter
{
	public static class AnimationCreator
    {

        public static void GenerateAnimation(Texture2D texture, float fps, bool generateGameObject)
        {
            var assetPath = AssetDatabase.GetAssetPath(texture);
            GenerateAnimation(assetPath, fps, generateGameObject);
        }

        public static void GenerateAnimation(string spriteSheetPath, float fps, bool generateGameObject)
        {
            string filename = Path.GetFileNameWithoutExtension(spriteSheetPath);
            string pathNoExtension = Path.Combine(Path.GetDirectoryName(spriteSheetPath), filename);

            var objects = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);
            var sprites = objects.Where(q => q is Sprite).Cast<Sprite>().ToArray();

            AnimationClip clip = UnityEditor.Animations.AnimatorController.AllocateAnimatorClip("clipName");
            clip.frameRate = fps;
            clip.wrapMode = WrapMode.Loop;

            EditorCurveBinding spriteBinding = new EditorCurveBinding();
            spriteBinding.type = typeof(SpriteRenderer);
            spriteBinding.path = "";
            spriteBinding.propertyName = "m_Sprite";
            ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < (sprites.Length); i++)
            {
				//Added security check:
				if(float.IsNaN(sprites[i].pivot.x)||float.IsNaN(sprites[i].pivot.y))
                {
                    Debug.Log("There is a problem with sprite: "+sprites[i].name+" (pivot is NaN)");
                }
				
                spriteKeyFrames[i] = new ObjectReferenceKeyframe();
                spriteKeyFrames[i].time = i / clip.frameRate;
                spriteKeyFrames[i].value = sprites[i];
            }
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames);

            AssetDatabase.CreateAsset(clip, pathNoExtension + ".anim");
            AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);


            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(spriteSheetPath + ".controller");
            var rootStateMachine = controller.layers[0].stateMachine;
            var state = rootStateMachine.AddState(filename);
            state.motion = clip;

            if (generateGameObject)
            {
                string gameObjectName = filename + "-Generated";

                GameObject go = GameObject.Find("/" + gameObjectName);
                if (go == null)
                {
                    go = new GameObject(gameObjectName);
                }

                GetOrAddComponent<SpriteRenderer>(go).sprite = sprites[0];
                GetOrAddComponent<Animator>(go).runtimeAnimatorController = controller;
            }
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component != null)
                return component;
            return go.AddComponent<T>();
        }
    }
}
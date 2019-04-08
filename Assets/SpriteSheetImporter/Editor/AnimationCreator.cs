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
        private struct SpriteSheetAnimationData
        {
            public string name;
            public float framesPerSecond;
            public Sprite[] sprites;
        }

        public static void GenerateAnimation(Texture2D texture, float fps, bool generateGameObject)
        {
            var assetPath = AssetDatabase.GetAssetPath(texture);
            GenerateAnimation(assetPath, fps, generateGameObject);
        }

        public static void GenerateAnimation(string spriteSheetPath, float fps, bool generateGameObject)
        {
            string filename = Path.GetFileNameWithoutExtension(spriteSheetPath);
            string directoryPath = Path.GetDirectoryName(spriteSheetPath);
            string pathNoExtension = Path.Combine(Path.GetDirectoryName(spriteSheetPath), filename);

            var objects = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);
            var sprites = objects.Where(q => q is Sprite).Cast<Sprite>().ToArray();

            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(pathNoExtension + "-Generated" + ".controller"); //Added "-Generated" to reduce overwrite risk
            var rootStateMachine = controller.layers[0].stateMachine;

            //subfolder creation to reduce overwrite risk of manualy created animations:
            string subdirectoryPath = Path.Combine(directoryPath, "Animations-Generated");
            if(!AssetDatabase.IsValidFolder(subdirectoryPath))
            {
                AssetDatabase.CreateFolder(directoryPath, "Animations-Generated"); 
            }

            var animations = GetAnimationSequence(sprites, fps);
            foreach (SpriteSheetAnimationData spriteAnimation in animations)
            {
                AnimationClip clip = CreateAnimationClip(spriteAnimation, subdirectoryPath); //Changed to subdirectoryPath to reduce overwrite risk

                var state = rootStateMachine.AddState(filename);
                state.name = spriteAnimation.name;
                state.motion = clip;
            }

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

        private static SpriteSheetAnimationData[] GetAnimationSequence(Sprite[] allSprites, float fps)
        {
            string name = null;
            string lastName = null;
            int? number;
            int? lastNumber = null;

            List<SpriteSheetAnimationData> spriteAnimations = new List<SpriteSheetAnimationData>();
            List<Sprite> spritesInAnimationClip = new List<Sprite>();
            foreach (Sprite sprite in allSprites)
            {
                // We we have a number, and it's equal to the last number minus one
                if (GetNumberAtEndOfString(sprite.name, out name, out number) && lastNumber != null && lastNumber == number - 1 && name == lastName)
                {
                    // Continue animation sequence
                    spritesInAnimationClip.Add(sprite);
                }
                else if (lastNumber == null && number == null)
                {
                    // There are no numbers at the end, but put them all together
                    // Or else we will end up with a lot of animations
                    spritesInAnimationClip.Add(sprite);
                }
                else
                {
                    // If we don't have a number, or number is out of order
                    // Push old animation sequence,
                    if (spritesInAnimationClip != null && spritesInAnimationClip.Count > 0)
                    {
                        spriteAnimations.Add(new SpriteSheetAnimationData()
                        {
                            framesPerSecond = fps,
                            sprites = spritesInAnimationClip.ToArray(),
                            name = lastName,
                        });
                    }
                    // start creating new animation sequence
                    spritesInAnimationClip = new List<Sprite>();
                    spritesInAnimationClip.Add(sprite);
                }
                lastName = name;
                lastNumber = number;
            }

            if (spritesInAnimationClip != null && spritesInAnimationClip.Count > 0)
            {
                spriteAnimations.Add(new SpriteSheetAnimationData()
                {
                    framesPerSecond = fps,
                    sprites = spritesInAnimationClip.ToArray(),
                    name = lastName,
                });
            }
            return spriteAnimations.ToArray();
        }

        private static AnimationClip CreateAnimationClip(SpriteSheetAnimationData spriteAnimation, string saveDirectoryPath)
        {
            AnimationClip clip = CreateAnimationClip(spriteAnimation);

            AssetDatabase.CreateAsset(clip, Path.Combine(saveDirectoryPath, spriteAnimation.name + ".anim"));
            AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = clip.isLooping;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

            return clip;
        }

        private static AnimationClip CreateAnimationClip(SpriteSheetAnimationData spriteAnimation)
        {
            AnimationClip clip = UnityEditor.Animations.AnimatorController.AllocateAnimatorClip(spriteAnimation.name);
            clip.frameRate = spriteAnimation.framesPerSecond;
            clip.wrapMode = WrapMode.Loop;

            EditorCurveBinding spriteBinding = new EditorCurveBinding();
            spriteBinding.type = typeof(SpriteRenderer);
            spriteBinding.path = "";
            spriteBinding.propertyName = "m_Sprite";
            ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[spriteAnimation.sprites.Length];
            for (int i = 0; i < (spriteAnimation.sprites.Length); i++)
            {
                //Added security check:
                if (float.IsNaN(spriteAnimation.sprites[i].pivot.x) || float.IsNaN(spriteAnimation.sprites[i].pivot.y))
                {
                    Debug.Log("There is a problem with sprite: " + spriteAnimation.sprites[i].name + " (pivot is NaN)");
                }

                spriteKeyFrames[i] = new ObjectReferenceKeyframe();
                spriteKeyFrames[i].time = i / clip.frameRate;
                spriteKeyFrames[i].value = spriteAnimation.sprites[i];
            }
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames);

            return clip;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component != null)
                return component;
            return go.AddComponent<T>();
        }

        public static bool GetNumberAtEndOfString(string input, out string name, out int? number)
        {
            int index = input.Length - 1;
            while (index > 0)
            {
                if (input[index] < 48 || input[index] > 57)
                {
                    // It's not a number
                    break;
                }
                index--;
            }
            if (index == (input.Length - 1))
            {
                name = input;
                number = null;
                return false;
            }

            index++;

            name = input.Substring(0, index);
            number = int.Parse(input.Substring(index));
            return true;
        }
    }
}
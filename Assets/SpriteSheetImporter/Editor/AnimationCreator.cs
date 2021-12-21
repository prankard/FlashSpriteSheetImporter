using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;
using System.Xml;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine.UI;

/// <summary>
/// Animation Creator, a helpful static class for generating animation based on tilesheets
/// </summary>

namespace Prankard.FlashSpriteSheetImporter
{
	public static class AnimationCreator
    {
        [System.Serializable]
        public enum RendererType
        {
            SpriteRenderer,
            Image
        }

        private struct SpriteSheetAnimationData
        {
            public string name;
            public float framesPerSecond;
            public Sprite[] sprites;
        }

        public static void GenerateAnimation(Texture2D texture, float fps, RendererType rendererType, bool createAnimationController, bool generateGameObject)
        {
            var assetPath = AssetDatabase.GetAssetPath(texture);
            GenerateAnimation(assetPath, fps, rendererType, createAnimationController, generateGameObject);
        }

        public static void GenerateAnimation(string spriteSheetPath, float fps, RendererType rendererType, bool createAnimationController, bool generateGameObject)
        {
            if (generateGameObject)
                createAnimationController = true;

            string filename = Path.GetFileNameWithoutExtension(spriteSheetPath);
            string directoryPath = Path.GetDirectoryName(spriteSheetPath);
            string pathNoExtension = Path.Combine(Path.GetDirectoryName(spriteSheetPath), filename);

            var objects = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);
            var sprites = objects.Where(q => q is Sprite).Cast<Sprite>().ToArray();

            //subfolder creation to reduce overwrite risk of manualy created animations:
            string subdirectoryPath = Path.Combine(directoryPath, "Animations-Generated");
            if(!AssetDatabase.IsValidFolder(subdirectoryPath))
            {
                AssetDatabase.CreateFolder(directoryPath, "Animations-Generated");
            }

            // Create the AnimatorController
            AnimatorStateMachine rootStateMachine = null;
            AnimatorController controller = null;
            if (createAnimationController)
            {
                controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(pathNoExtension + "-Generated" + ".controller"); //Added "-Generated" to reduce overwrite risk
                rootStateMachine = controller.layers[0].stateMachine;
            }

            // Create the AnimationClips (and push to controller)
            var animations = GetAnimationSequence(sprites, fps);
            foreach (SpriteSheetAnimationData spriteAnimation in animations)
            {
                AnimationClip clip = CreateAnimationClip(spriteAnimation, rendererType, subdirectoryPath); //Changed to subdirectoryPath to reduce overwrite risk

                if (rootStateMachine == null)
                    continue;

                var state = rootStateMachine.AddState(filename);
                state.name = spriteAnimation.name;
                state.motion = clip;
            }

            // Create the GameObject
            if (controller != null && generateGameObject)
            {
                GameObject parent = null;
                if (rendererType == RendererType.Image)
                {
                    parent = GameObject.Find("/SpriteSheetImporterCanvas");
                    if (parent == null)
                    {
                        parent = new GameObject("SpriteSheetImporterCanvas");
                        var canvas = parent.gameObject.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    }    
                }

                string gameObjectName = filename + "-Generated";
                GameObject go = GameObject.Find("/" + gameObjectName);
                if (go == null)
                {
                    go = new GameObject(gameObjectName);
                    if (parent != null)
                        go.transform.SetParent(parent.transform);
                }

                switch (rendererType)
                {
                    case RendererType.Image:
                        var image = GetOrAddComponent<Image>(go).sprite = sprites[0];
                        if (sprites[0] != null && sprites[0].rect.size.sqrMagnitude > 0) // First frame could be blank
                        {
                            var rt = GetOrAddComponent<RectTransform>(go);
                            rt.anchoredPosition = Vector2.zero;
                            rt.pivot = new Vector2(
                                sprites[0].pivot.x / sprites[0].rect.size.x,
                                sprites[0].pivot.y / sprites[0].rect.size.y
                            );
                            rt.sizeDelta = sprites[0].rect.size;
                        }
                        break;
                    case RendererType.SpriteRenderer:
                        GetOrAddComponent<SpriteRenderer>(go).sprite = sprites[0];
                        break;
                }
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

        private static AnimationClip CreateAnimationClip(SpriteSheetAnimationData spriteAnimation, RendererType rendererType, string saveDirectoryPath)
        {
            AnimationClip clip = CreateAnimationClip(spriteAnimation, rendererType);

            AssetDatabase.CreateAsset(clip, Path.Combine(saveDirectoryPath, spriteAnimation.name + ".anim"));
            AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = clip.isLooping;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

            return clip;
        }

        private static AnimationClip CreateAnimationClip(SpriteSheetAnimationData spriteAnimation, RendererType rendererType)
        {
            AnimationClip clip = UnityEditor.Animations.AnimatorController.AllocateAnimatorClip(spriteAnimation.name);
            clip.frameRate = spriteAnimation.framesPerSecond;
            clip.wrapMode = WrapMode.Loop;

            //Added security check:
            foreach (var sprite in spriteAnimation.sprites)
            {
                if (float.IsNaN(sprite.pivot.x) || float.IsNaN(sprite.pivot.y))
                {
                    Debug.Log("There is a problem with sprite: " + sprite.name + " (pivot is NaN)");
                }
            }

            // Unassign empty frames
            for (int i = 0; i < spriteAnimation.sprites.Length; i++)
            {
                if (spriteAnimation.sprites[i].rect.width == 0 || spriteAnimation.sprites[i].rect.height == 0)
                {
                    spriteAnimation.sprites[i] = null;
                }
            }

            System.Type type = null;
            switch (rendererType)
            {
                case RendererType.SpriteRenderer:
                    type = typeof(SpriteRenderer);
                    break;
                case RendererType.Image:
                    type = typeof(Image);
                    break;
            }

            AddKeyframes(clip, "m_Sprite", type, spriteAnimation.sprites, "");

            // Add extra keyframes for image, which aren't handled normally
            if (rendererType == RendererType.Image)
            {
                var widths = new List<float>();
                var heights = new List<float>();
                var pivotXs = new List<float>();
                var pivotYs = new List<float>();
                foreach (var sprite in spriteAnimation.sprites)
                {
                    // Handle blank frames (zero width or zero height)
                    if (sprite == null)
                    {
                        heights.Add(0); widths.Add(0); pivotXs.Add(0); pivotYs.Add(0);
                        continue;
                    }
                    Debug.Log(sprite.rect.width);
                    Debug.Log(sprite.rect.height);
                    Debug.Log(sprite.rect.size);
                    Debug.Log("--");
                    widths.Add(sprite.rect.size.x);
                    heights.Add(sprite.rect.size.y);
                    pivotXs.Add(sprite.pivot.x / sprite.rect.size.x);
                    pivotYs.Add(sprite.pivot.y / sprite.rect.size.y);
                }
                AddKeyframes(clip, "m_SizeDelta.x", typeof(RectTransform), widths.ToArray(), "");
                AddKeyframes(clip, "m_SizeDelta.y", typeof(RectTransform), heights.ToArray(), "");
                AddKeyframes(clip, "m_Pivot.x", typeof(RectTransform), pivotXs.ToArray(), "");
                AddKeyframes(clip, "m_Pivot.y", typeof(RectTransform), pivotYs.ToArray(), "");
            }

            return clip;
        }

        private static void AddKeyframes(AnimationClip clip, string propertyName, System.Type propertyType, float[] values, string transformPath = "")
        {
            var binding = GetKeyframeBindings(propertyName, propertyType, transformPath);

            List<Keyframe> keyFrames = new List<Keyframe>();
            for (int i = 0; i < (values.Length); i++)
            {
                var keyframe = new Keyframe();
                var keyframe2 = new Keyframe();
                keyframe.time = i / clip.frameRate;
                keyframe2.time = (i + 0.999f) / clip.frameRate;
                keyframe2.value = keyframe.value = values[i];
                keyframe2.inTangent = keyframe2.outTangent = keyframe.inTangent = keyframe.outTangent = 0; // linear (no curve)
                keyFrames.Add(keyframe);
                keyFrames.Add(keyframe2);
            }

            AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(keyFrames.ToArray()));
        }

        private static void AddKeyframes(AnimationClip clip, string propertyName, System.Type propertyType, UnityEngine.Object[] values, string transformPath = "")
        {
            var binding = GetKeyframeBindings(propertyName, propertyType, transformPath);

            ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[values.Length];
            for (int i = 0; i < (values.Length); i++)
            {
                keyFrames[i] = new ObjectReferenceKeyframe();
                keyFrames[i].time = i / clip.frameRate;
                keyFrames[i].value = values[i];
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyFrames);
        }

        private static EditorCurveBinding GetKeyframeBindings(string propertyName, System.Type propertyType, string transformPath = "")
        {
            return new EditorCurveBinding()
            {
                type = propertyType,
                path = transformPath,
                propertyName = propertyName
            };
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
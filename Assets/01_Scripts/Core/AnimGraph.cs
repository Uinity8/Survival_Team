using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace _01_Scripts.Core
{
    public enum Mask
    {
        Hand,
        UpperBody,
        RightFinger
    }

    public class AnimGraph : MonoBehaviour
    {
        PlayableGraph graph;
        AnimationPlayableOutput output;
        AnimationMixerPlayable actionMixer;
        AnimationMixerPlayable animatorOutput;
        AnimatorControllerPlayable playableAnimator;

        public Animator animator;
        AvatarMask handMask;
        AvatarMask upperBodyMask;
        AvatarMask rightFingerMask;
        Dictionary<string, object> savedParameters;

        bool maskRemoving;
        bool maskAdding;
        readonly List<AnimationLayerMixerPlayable> layerMixerList = new();

        public void Awake()
        {
            savedParameters = new Dictionary<string, object>();
            CreateGraph();
        }

        private void Update()
        {
            if (animator != null)
            {
                if (Math.Abs(playableAnimator.GetSpeed() - animator.speed) > 0f)
                    playableAnimator.SetSpeed(animator.speed);
            }
        }


        void CreateGraph(AnimatorOverrideController runtimeAnimatorController = null)
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            SetupAnimationGraph();
            CreateDefaultMask();

            playableAnimator = AnimatorControllerPlayable.Create(graph,
                runtimeAnimatorController == null ? animator.runtimeAnimatorController : runtimeAnimatorController);
            actionMixer = AnimationMixerPlayable.Create(graph, 2);
            animatorOutput = AnimationMixerPlayable.Create(graph, 2);
            animatorOutput.SetInputWeight(0, 1);
            animatorOutput.SetInputWeight(1, 0);
            graph.Connect(playableAnimator, 0, animatorOutput, 0);
            graph.Connect(animatorOutput, 0, actionMixer, 0);

            actionMixer.SetInputWeight(0, 1);
            actionMixer.SetInputWeight(1, 0);
            output.SetSourcePlayable(actionMixer);

            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            graph.Play();
        }

        public void CrossFade(AnimationClip clip, float transitionIn = 0.2f,
            bool transitionBack = true, float transitionOut = 0.2f, float animationSpeed = 1,
            Action<float, float> onAnimationUpdate = null, Action onComplete = null, AnimGraphClipInfo clipInfo = null)
        {
            StartCoroutine(CrossFadeAsync(clip, transitionIn, transitionBack, transitionOut, animationSpeed,
                onAnimationUpdate, onComplete, clipInfo));
        }

        public class ClipStateInfo
        {
            public AnimGraphClipInfo CurrentClipInfo;
            public float NormalizedTime = 0f;
            public float Timer = 0f;
            public float DeltaTime = 0f;
            public float ClipLength = 0f;
            public bool IsPlayingAnimation => CurrentClipInfo != null;
        }

        public readonly ClipStateInfo CurrentClipStateInfo = new();
        public AnimGraphClipInfo CurrentClipInfo => CurrentClipStateInfo.CurrentClipInfo;

        float transitionTime;

        public IEnumerator CrossFadeAsync(AnimationClip clip, float transitionIn = 0.2f,
            bool transitionBack = true, float transitionOut = 0.2f, float animationSpeed = 1,
            Action<float, float> onAnimationUpdate = null, Action onComplete = null, AnimGraphClipInfo clipInfo = null)
        {
            if (clipInfo != null)
            {
                if (clip == null) clip = clipInfo.clip;

                if (clipInfo.clip != null)
                {
                    transitionIn = clipInfo.TransitionInAndOut.x;
                    transitionOut = clipInfo.TransitionInAndOut.y;
                }
            }

            if (clip == null)
            {
                Debug.Log("no clip found");
                yield break;
            }

            var clipPlayable = AnimationClipPlayable.Create(graph, clip);
            clipPlayable.SetSpeed(animationSpeed);
            var source = actionMixer.GetInput(0);
            actionMixer.DisconnectInput(0);

            var clipMixer = AnimationMixerPlayable.Create(graph, 2);
            clipMixer.ConnectInput(0, source, 0);
            clipMixer.ConnectInput(1, clipPlayable, 0);

            actionMixer.ConnectInput(0, clipMixer, 0);

            actionMixer.SetInputWeight(0, 1);
            actionMixer.SetInputWeight(1, 0);
            var animationTime = transitionIn;

            yield return UpdateWeights(clip, clipPlayable, clipMixer, transitionIn, transitionBack, transitionOut,
                animationSpeed, clipInfo, onAnimationUpdate);
            transitionTime = animationTime;
            yield return new WaitUntil(() => actionMixer.GetInput(0).Equals(clipMixer));

            if (transitionBack)
            {
                //TransitionBack();
                var currInput = clipMixer.GetInput(0);
                var currOutput = clipMixer.GetOutput(0);
                clipMixer.DisconnectInput(0);

                if (!currOutput.IsNull())
                {
                    currOutput.DisconnectInput(0);
                    currOutput.ConnectInput(0, currInput, 0);

                    currOutput.SetInputWeight(0, 1);
                    currOutput.SetInputWeight(1, 0);
                }

                clipMixer.Destroy();
                clipPlayable.Destroy();
            }

            onComplete?.Invoke();
        }

        public bool StopLoopingClip { get; set; }

        public void CrossFadeAndLoop(AnimationClip clip, float transitionIn = 0.2f,
            bool transitionBack = true, float transitionOut = 0.2f)
        {
            StartCoroutine(CrossFadeAndLoopAsync(clip, transitionIn, transitionBack, transitionOut));
        }

        private IEnumerator CrossFadeAndLoopAsync(AnimationClip clip, float transitionIn = 0.2f,
            bool transitionBack = true, float transitionOut = 0.2f)
        {
            StopLoopingClip = false;

            if (clip == null)
            {
                Debug.Log("no clip found");
                yield break;
            }

            var clipPlayable = AnimationClipPlayable.Create(graph, clip);
            var source = actionMixer.GetInput(0);
            actionMixer.DisconnectInput(0);

            var clipMixer = AnimationMixerPlayable.Create(graph, 2);
            clipMixer.ConnectInput(0, source, 0);
            clipMixer.ConnectInput(1, clipPlayable, 0);

            actionMixer.ConnectInput(0, clipMixer, 0);

            actionMixer.SetInputWeight(0, 1);
            actionMixer.SetInputWeight(1, 0);

            float timer = 0f;
            float weight;

            // Transition In
            while (timer <= transitionIn)
            {
                weight = Mathf.Lerp(0, 1, timer / transitionIn);

                clipMixer.SetInputWeight(0, 1 - weight);
                clipMixer.SetInputWeight(1, weight);

                yield return null;
                timer += Time.deltaTime;
            }

            // Keep looping
            yield return new WaitUntil(() => StopLoopingClip);

            // Transition Out
            timer = 0f;
            while (timer <= transitionOut)
            {
                if (transitionBack)
                {
                    weight = Mathf.Lerp(1, 0, timer / transitionIn);

                    clipMixer.SetInputWeight(0, 1 - weight);
                    clipMixer.SetInputWeight(1, weight);
                }

                yield return null;
                timer += Time.deltaTime;
            }

            // Remove animation clip
            var currInput = clipMixer.GetInput(0);
            var currOutput = clipMixer.GetOutput(0);
            clipMixer.DisconnectInput(0);

            if (!currOutput.IsNull())
            {
                currOutput.DisconnectInput(0);
                currOutput.ConnectInput(0, currInput, 0);

                currOutput.SetInputWeight(0, 1);
                currOutput.SetInputWeight(1, 0);
            }

            clipMixer.Destroy();
            clipPlayable.Destroy();
        }

        public void TransitionBack()
        {
            var clipMixer = actionMixer.GetInput(0);
            var clipPlayable = clipMixer.GetInput(1);

            var currInput = clipMixer.GetInput(0);
            var currOutput = clipMixer.GetOutput(0);
            clipMixer.DisconnectInput(0);

            if (!currOutput.IsNull())
            {
                currOutput.DisconnectInput(0);
                currOutput.ConnectInput(0, currInput, 0);

                currOutput.SetInputWeight(0, 1);
                currOutput.SetInputWeight(1, 0);
            }

            clipMixer.Destroy();
            clipPlayable.Destroy();
        }

        public void TransitionBackFully()
        {
            var clipMixer = actionMixer.GetInput(0);
            var clipPlayable = clipMixer.GetInput(1);

            var currOutput = clipMixer.GetOutput(0);
            clipMixer.DisconnectInput(0);

            if (!currOutput.IsNull())
            {
                var animatorOut = playableAnimator.GetOutput(0);
                animatorOut.DisconnectInput(0);

                currOutput.DisconnectInput(0);
                currOutput.ConnectInput(0, playableAnimator, 0);

                currOutput.SetInputWeight(0, 1);
                currOutput.SetInputWeight(1, 0);
            }

            clipMixer.Destroy();
            clipPlayable.Destroy();
        }

        public IEnumerator CrosseFadeOverrideController(AnimatorOverrideController overrideController, float duration)
        {
            if (overrideController == null) yield break;
            playableAnimator = AnimatorControllerPlayable.Create(graph, overrideController);
            animatorOutput = AnimationMixerPlayable.Create(graph, 2);
            animatorOutput.SetInputWeight(0, 1);
            animatorOutput.SetInputWeight(1, 0);
            graph.Connect(playableAnimator, 0, animatorOutput, 0);


            var source = actionMixer.GetInput(0);
            actionMixer.DisconnectInput(0);

            var clipMixer = AnimationMixerPlayable.Create(graph, 2);
            clipMixer.ConnectInput(0, source, 0);
            clipMixer.ConnectInput(1, animatorOutput, 0);

            actionMixer.ConnectInput(0, clipMixer, 0);

            actionMixer.SetInputWeight(0, 1);
            actionMixer.SetInputWeight(1, 0);

            if (duration > 0)
            {
                SaveAnimatorParameters();
                clipMixer.SetInputWeight(0, 1);
                clipMixer.SetInputWeight(1, 0);
                yield return null;
                RestoreAnimatorParameters();
            }

            float timer = 0f;
            while (timer < duration)
            {
                float weight = Mathf.Lerp(0, 1, timer / duration);
                clipMixer.SetInputWeight(0, 1 - weight);
                clipMixer.SetInputWeight(1, weight);
                timer += Time.deltaTime;
                yield return null;
            }

            clipMixer.SetInputWeight(0, 0);
            clipMixer.SetInputWeight(1, 1);
            clipMixer.DisconnectInput(0);
            clipMixer.Destroy();
            actionMixer.DisconnectInput(0);
            graph.Connect(animatorOutput, 0, actionMixer, 0);
            actionMixer.SetInputWeight(0, 1);
            actionMixer.SetInputWeight(1, 0);
            animator.runtimeAnimatorController = overrideController;
        }

        private static KeyValuePair<string, float> _timeScaleOwner = new("", 1);

        IEnumerator UpdateWeights(AnimationClip clip, AnimationClipPlayable clipPlayable, AnimationMixerPlayable mixer,
            float transitionIn,
            bool transitionBack, float transitionOut, float animationSpeed = 1, AnimGraphClipInfo clipInfo = null,
            Action<float, float> onAnimationUpdate = null)
        {
            if (clipInfo != null)
            {
                clip = clipInfo.clip != null ? clipInfo.clip : clip;
            }

            float timer = 0f;
            float normalizedTimer = 0f;
            float weight = 0f;
            bool animationOverriding = false;
            var clipLength = clip.length;

            clipInfo ??= new AnimGraphClipInfo
            {
                clip = clip
            };

            CurrentClipStateInfo.CurrentClipInfo = clipInfo;
            CurrentClipStateInfo.NormalizedTime = normalizedTimer;
            CurrentClipStateInfo.ClipLength = clipLength;
            CurrentClipStateInfo.Timer = timer;

            var uniqueId = Guid.Empty.ToString();

            if (clipInfo.customAnimationSpeed && clipInfo.useAsGlobalTimeScale)
            {
                _timeScaleOwner = _timeScaleOwner.Key != ""
                    ? new KeyValuePair<string, float>(uniqueId, _timeScaleOwner.Value)
                    : new KeyValuePair<string, float>(uniqueId, Time.timeScale);
            }
            //print(TimeScaleOwner.Value);

            var currentTimescale = _timeScaleOwner.Value;
            while (timer <= clipLength)
            {
                if (clipInfo.customAnimationSpeed)
                {
                    if (clipInfo.useAsGlobalTimeScale)
                    {
                        if (_timeScaleOwner.Key == uniqueId)
                            Time.timeScale = currentTimescale *
                                             Mathf.Max(clipInfo.speedModifier.GetValue(normalizedTimer), 0.01f);
                        animationSpeed = 1;
                    }
                    else
                    {
                        animationSpeed = Mathf.Max(clipInfo.speedModifier.GetValue(normalizedTimer), 0.01f);
                        mixer.SetSpeed(animationSpeed);
                    }
                }

                onAnimationUpdate?.Invoke(normalizedTimer, timer);

                normalizedTimer = timer / clipLength;
                CurrentClipStateInfo.NormalizedTime = normalizedTimer;

                if (timer <= transitionIn)
                {
                    weight = Mathf.Lerp(0, 1, timer / transitionIn);
                }
                else if (transitionBack && timer > clipLength - transitionOut)
                {
                    weight = Mathf.Lerp(1, 0, (timer - (clipLength - transitionOut)) / transitionOut);
                }

                mixer.SetInputWeight(0, 1 - weight);
                mixer.SetInputWeight(1, weight);

                foreach (var item in clipInfo.events)
                {
                    if (item.normalizedTime >= normalizedTimer &&
                        item.normalizedTime <= normalizedTimer + Time.deltaTime * animationSpeed)
                    {
                        item.InvokeCustomAnimationEvent(this.gameObject);
                    }
                }

                if (!actionMixer.GetInput(0).Equals(mixer) && transitionBack && StopLoopingClip)
                {
                    animationOverriding = true;
                    break;
                }

                yield return null;
                timer += Time.deltaTime * animationSpeed;
                CurrentClipStateInfo.Timer = timer;
                CurrentClipStateInfo.DeltaTime = Time.deltaTime * animationSpeed;
            }

            foreach (var item in clipInfo.onEndAnimation)
            {
                item.InvokeCustomAnimationEvent(this.gameObject);
            }

            CurrentClipStateInfo.CurrentClipInfo = null;

            if (_timeScaleOwner.Key == uniqueId && clipInfo.customAnimationSpeed && clipInfo.useAsGlobalTimeScale)
            {
                Time.timeScale = currentTimescale;
                _timeScaleOwner = new KeyValuePair<string, float>("", Time.timeScale);
            }

            if (animationOverriding)
            {
                timer = 0;
                while (timer <= transitionTime + transitionIn)
                {
                    if (timer <= transitionTime)
                    {
                        timer += Time.deltaTime;
                        yield return null;
                        continue;
                    }

                    weight = Mathf.Lerp(weight, 1, timer / transitionIn);
                    mixer.SetInputWeight(0, weight);
                    mixer.SetInputWeight(1, 1 - weight);
                    yield return null;
                    timer += Time.deltaTime;
                }
            }
        }

        public void CrossFadeAvatarMaskAnimation(AnimationClip clip, Mask targetMask = Mask.Hand,
            bool transitionBack = false, AvatarMask mask = null, float transitionInTime = 0f,
            bool removeMaskAfterComplete = true, float animationSpeed = 1)
        {
            StartCoroutine(CrossFadeAvatarMaskAnimationAsync(clip, targetMask, transitionBack, mask, transitionInTime,
                removeMaskAfterComplete, animationSpeed));
        }

        private IEnumerator CrossFadeAvatarMaskAnimationAsync(AnimationClip clip, Mask targetMask = Mask.Hand,
            bool transitionBack = false, AvatarMask mask = null, float transitionInTime = 0f,
            bool removeMaskAfterComplete = true, float animationSpeed = 1)
        {
            maskAdding = true;
            yield return new WaitUntil(() => maskRemoving == false);
            if (clip == null)
            {
                maskAdding = false;
                yield break;
            }

            if (mask == null)
            {
                switch (targetMask)
                {
                    case Mask.Hand:
                        mask = handMask;
                        break;
                    case Mask.UpperBody:
                        mask = upperBodyMask;
                        break;
                    case Mask.RightFinger:
                        mask = rightFingerMask;
                        break;
                }
            }

            layerMixerList.Add(AnimationLayerMixerPlayable.Create(graph, 2));
            var layerMixer = layerMixerList.Last();
            var clipPlayable = AnimationClipPlayable.Create(graph, clip);
            clipPlayable.SetSpeed(animationSpeed);
            var source = actionMixer.GetInput(0);
            actionMixer.DisconnectInput(0);

            layerMixer.ConnectInput(0, source, 0);
            layerMixer.ConnectInput(1, clipPlayable, 0);

            actionMixer.ConnectInput(0, layerMixer, 0);
            actionMixer.SetInputWeight(0, 1);
            actionMixer.SetInputWeight(1, 0);

            layerMixer.SetLayerMaskFromAvatarMask(1, mask);
            layerMixer.SetInputWeight(0, 1f);

            float timer = 0f;
            transitionInTime /= animationSpeed;
            while (timer < transitionInTime && layerMixer.IsValid() && !maskRemoving)
            {
                float weight = Mathf.Lerp(0, 1, timer / transitionInTime);
                layerMixer.SetInputWeight(1, weight);
                timer += Time.deltaTime;
                yield return null;
            }

            if (layerMixer.IsValid())
                layerMixer.SetInputWeight(1, 1);

            if (transitionBack)
            {
                yield return new WaitForSeconds(clip.length / animationSpeed - transitionInTime);
                if (removeMaskAfterComplete)
                    RemoveAvatarMask();
            }

            maskAdding = false;
        }


        public void RemoveAvatarMask(float duration = .2f, bool removeAllLayer = false)
        {
            StartCoroutine(RemoveAvatarMaskAsync(duration, removeAllLayer));
        }

        private IEnumerator RemoveAvatarMaskAsync(float duration = .2f, bool removeAllLayer = false)
        {
            maskRemoving = true;
            yield return new WaitUntil(() => maskAdding == false);
            for (int i = layerMixerList.Count - 1; i >= 0; i--)
            {
                var layerMixer = layerMixerList[i];
                if (layerMixer.IsValid())
                {
                    float timer = 0f;
                    while (timer < duration && layerMixer.IsValid() && !maskAdding)
                    {
                        float weight = Mathf.Lerp(0, 1, timer / duration);
                        layerMixer.SetInputWeight(1, 1 - weight);
                        timer += Time.deltaTime;
                        yield return null;
                    }

                    if (layerMixer.IsValid())
                    {
                        layerMixer.SetInputWeight(1, 0);
                        var currInput = layerMixer.GetInput(0);
                        var currOutput = layerMixer.GetOutput(0);
                        layerMixer.DisconnectInput(0);

                        if (!currOutput.IsNull())
                        {
                            currOutput.DisconnectInput(0);
                            currOutput.ConnectInput(0, currInput, 0);

                            currOutput.SetInputWeight(0, 1);
                            currOutput.SetInputWeight(1, 0);
                        }

                        layerMixer.Destroy();
                    }
                }

                if (i < layerMixerList.Count)
                    layerMixerList.RemoveAt(i);
                if (!removeAllLayer)
                    break;
            }

            maskRemoving = false;
        }

        void CreateDefaultMask()
        {
            handMask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                handMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);

            handMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            handMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            handMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);
            handMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            handMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            handMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);

            upperBodyMask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                upperBodyMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);

            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);

            rightFingerMask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                rightFingerMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            rightFingerMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
        }


        public IEnumerator CrossFadeBlendAnimationAsync(AnimationClip clip1, AnimationClip clip2, float clip1Weight,
            float transitionIn = 0.2f, bool transitionBack = true, float transitionOut = 0.2f)
        {
            if (clip1 == null || clip2 == null)
            {
                Debug.Log("no clip found");
                yield break;
            }

            var clipPlayable1 = AnimationClipPlayable.Create(graph, clip1);
            var clipPlayable2 = AnimationClipPlayable.Create(graph, clip2);
            var source = actionMixer.GetInput(0);
            actionMixer.DisconnectInput(0);

            var clipPlayable = AnimationMixerPlayable.Create(graph, 2);
            clipPlayable.ConnectInput(0, clipPlayable1, 0);
            clipPlayable.ConnectInput(1, clipPlayable2, 0);

            clipPlayable.SetInputWeight(0, clip1Weight);
            clipPlayable.SetInputWeight(1, 1 - clip1Weight);

            var clipMixer = AnimationMixerPlayable.Create(graph, 2);
            clipMixer.ConnectInput(0, source, 0);
            clipMixer.ConnectInput(1, clipPlayable, 0);

            actionMixer.ConnectInput(0, clipMixer, 0);

            actionMixer.SetInputWeight(0, 1);
            actionMixer.SetInputWeight(1, 0);

            var animationTime = transitionIn;

            yield return UpdateWeights(clip1, clipPlayable1, clipMixer, transitionIn, transitionBack, transitionOut);
            transitionTime = animationTime;
            yield return new WaitUntil(() => actionMixer.GetInput(0).Equals(clipMixer));

            if (transitionBack)
            {
                var currInput = clipMixer.GetInput(0);
                var currOutput = clipMixer.GetOutput(0);
                clipMixer.DisconnectInput(0);

                if (!currOutput.IsNull())
                {
                    currOutput.DisconnectInput(0);
                    currOutput.ConnectInput(0, currInput, 0);

                    currOutput.SetInputWeight(0, 1);
                    currOutput.SetInputWeight(1, 0);
                }

                clipMixer.Destroy();
                clipPlayable.Destroy();
            }
        }


        public IEnumerator CrossFadeBlendAnimationAsyncLayer(AnimationClip clip1, AnimationClip clip2,
            float clip1Weight, Mask targetMask = Mask.Hand, bool transitionBack = false, AvatarMask mask = null,
            float duration = 0f, bool removeMaskAfterComplete = true)
        {
            maskAdding = true;
            yield return new WaitUntil(() => maskRemoving == false);
            if (clip1 == null || clip2 == null)
            {
                Debug.Log("no clip found");
                yield break;
            }

            if (mask == null)
            {
                switch (targetMask)
                {
                    case Mask.Hand:
                        mask = handMask;
                        break;
                    case Mask.UpperBody:
                        mask = upperBodyMask;
                        break;
                }
            }

            layerMixerList.Add(AnimationLayerMixerPlayable.Create(graph, 2));
            var layerMixer = layerMixerList.Last();


            var clipPlayable1 = AnimationClipPlayable.Create(graph, clip1);
            var clipPlayable2 = AnimationClipPlayable.Create(graph, clip2);
            var source = actionMixer.GetInput(0);
            actionMixer.DisconnectInput(0);

            var clipPlayable = AnimationMixerPlayable.Create(graph, 2);
            clipPlayable.ConnectInput(0, clipPlayable1, 0);
            clipPlayable.ConnectInput(1, clipPlayable2, 0);

            clipPlayable.SetInputWeight(0, clip1Weight);
            clipPlayable.SetInputWeight(1, 1 - clip1Weight);


            layerMixer.ConnectInput(0, source, 0);
            layerMixer.ConnectInput(1, clipPlayable, 0);

            actionMixer.ConnectInput(0, layerMixer, 0);

            actionMixer.SetInputWeight(0, 1);
            actionMixer.SetInputWeight(1, 0);


            layerMixer.SetLayerMaskFromAvatarMask(1, mask);
            layerMixer.SetInputWeight(0, 1f);

            float timer = 0f;
            while (timer < duration && layerMixer.IsValid() && !maskRemoving)
            {
                float weight = Mathf.Lerp(0, 1, timer / duration);
                layerMixer.SetInputWeight(1, weight);
                timer += Time.deltaTime;
                yield return null;
            }

            if (layerMixer.IsValid())
                layerMixer.SetInputWeight(1, 1);

            if (transitionBack)
            {
                yield return new WaitForSeconds(clip1.length - duration);

                if (removeMaskAfterComplete)
                    RemoveAvatarMask();
            }

            maskAdding = false;
        }


        void SetupAnimationGraph()
        {
            graph = PlayableGraph.Create(gameObject.name + " graph");
            output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        }

        private void SaveAnimatorParameters()
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (animator.IsParameterControlledByCurve(parameter.nameHash)) continue;
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        savedParameters[parameter.name] = animator.GetFloat(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Int:
                        savedParameters[parameter.name] = animator.GetInteger(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        savedParameters[parameter.name] = animator.GetBool(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        savedParameters[parameter.name] = animator.GetBool(parameter.name);
                        break;
                }
            }
        }

        private void RestoreAnimatorParameters()
        {
            foreach (var parameter in savedParameters)
            {
                if (parameter.Value is float floating)
                {
                    animator.SetFloat(parameter.Key, floating);
                }
                else if (parameter.Value is int integer)
                {
                    animator.SetInteger(parameter.Key, integer);
                }
                else if (parameter.Value is bool boolean)
                {
                    animator.SetBool(parameter.Key, boolean);
                }
            }
        }

        private void OnDestroy()
        {
            graph.Destroy();
        }


        //private void OnGUI()
        //{
        //    var style = new GUIStyle() { fontSize = 24 };
        //     GUILayout.Label(layerMixerList.Count.ToString(), style);
        //     //GUILayout.Label("Weight = " + weight, style);
        //}
    }
}
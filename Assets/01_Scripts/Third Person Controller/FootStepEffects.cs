using System.Collections.Generic;
using System.Linq;
using _01_Scripts.System;
using AnimatorHash;
using Framework.Audio;
using UnityEngine;

namespace _01_Scripts.Third_Person_Controller
{
    // FootStepEffects 클래스는 발 착지 효과(소리와 파티클)를 처리하는 기능을 담당합니다.
    public class FootStepEffects : MonoBehaviour
    {
        // 발 착지 시 재생할 오디오 클립 목록
        [SerializeField] List<AudioClip> footStepSounds;
        // 발 착지 시 생성할 파티클 이펙트 프리팹 목록
        [SerializeField] List<GameObject> footStepParticles;

        // 발 착지 효과를 재정의할 때 사용할 설정 목록
        [SerializeField] List<OverrideStepEffects> overrideStepEffects;
        // 재정의할 기준 타입 (재질명, 텍스처명, 태그)
        [SerializeField] StepEffectsOverrideType overrideType;

        // 지면으로 간주할 레이어 마스크 (기본값 1)
        public LayerMask groundLayer = 1;

        // 소리 재생을 무시할 시스템 상태 목록 (예: 전투 중에는 발 소리 미재생 등)
        [SerializeField] List<SystemState> soundIgnoreStates;
        // 파티클 재생을 무시할 시스템 상태 목록
        [SerializeField] List<SystemState> particleIgnoreStates;

        // 이동 속도에 따라 볼륨을 조절할지 여부
        [SerializeField] bool adjustVolumeBasedOnSpeed = true;
        // 최소 볼륨 값
        [SerializeField] float minVolume = 0.2f;

        // 외부에서 재정의된 발 착지 효과 설정 목록을 접근할 수 있도록 하는 프로퍼티
        public List<OverrideStepEffects> OverrideStepEffects => overrideStepEffects;
        // 외부에서 재정의 타입을 접근할 수 있도록 하는 프로퍼티
        public StepEffectsOverrideType OverrideType => overrideType;

        // 플레이어 컨트롤러 컴포넌트 참조
        PlayerController playerController;
        // 애니메이터 컴포넌트 참조
        Animator animator;

        // Awake: 컴포넌트 초기화. PlayerController와 Animator 컴포넌트를 가져옵니다.
        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            animator = GetComponent<Animator>();
        }

        // Start: 발 착지 충돌 처리를 위해 FootTrigger 레이어와의 충돌 무시 설정 및 overrideStepEffects의 텍스트를 소문자로 변환합니다.
        private void Start()
        {
            // FootTrigger 레이어 번호를 가져옵니다.
            int footTriggerLayer = LayerMask.NameToLayer("FootTrigger");

            // 0부터 31까지의 모든 레이어에 대해 FootTrigger와의 충돌을 설정합니다.
            for (int i = 0; i < 32; i++)
            {
                // groundLayer에 포함되지 않은 레이어와의 충돌은 무시합니다.
                Physics.IgnoreLayerCollision(footTriggerLayer, i, !IsLayerInLayerMask(i, groundLayer));
            }

            // overrideStepEffects가 있다면, 각 OverrideStepEffects의 MaterialName을 소문자로 변경합니다.
            if (overrideStepEffects != null)
            {
                overrideStepEffects.ForEach(x => x.MaterialName = x.MaterialName.ToLower());
            }
        }

        // OnFootLand: 발이 지면에 착지했을 때 호출되어 소리와 파티클 효과를 재생합니다.
        // footTransform: 착지한 발의 Transform, floorData: 바닥의 정보를 담은 데이터 (옵션)
        public void OnFootLand(Transform footTransform, FloorStepData floorData = null)
        {
            // 기본 발 착지 소리와 파티클 이펙트 설정
            var sounds = footStepSounds;
            var particleEffects = footStepParticles;

            // 만약 바닥 데이터와 override 설정이 있다면, 해당 정보를 이용해 재정의 효과를 가져옵니다.
            if (floorData != null && overrideStepEffects != null)
            {
                // (디버그 로그 주석 처리됨)
                var overrideEffect = GetOverrideEffect(floorData);
                if (overrideEffect != null)
                {
                    // 재정의 설정에 따라 소리와 파티클 이펙트를 교체
                    if (overrideEffect.overrideFootStepSounds)
                        sounds = overrideEffect.footStepSounds;

                    if (overrideEffect.overrideFootStepParticles)
                        particleEffects = overrideEffect.footStepParticles;
                }
            }

            // 소리 효과 처리
            if(sounds is { Count: > 0 })
            {
                // playerController가 있고, 현재 시스템 상태가 소리 무시 목록에 없다면 소리 재생
                if (playerController != null && !soundIgnoreStates.Contains(playerController.FocusedSystemState))
                {
                    //여기에 낙하중 애니메이션이면 Land 사운드나게 추가해야함!!!!
                    
                  
                    float moveAmount = 1;
                    // 속도에 따른 볼륨 조절 옵션이 활성화되어 있으면 볼륨을 조정
                    if (adjustVolumeBasedOnSpeed) 
                    {
                        // Locomotion 상태에서 "Locomotion" 애니메이션이면 moveAmount 값을 애니메이터 파라미터에서 가져와 조정
                        if (playerController.CurrentSystemState == SystemState.Locomotion &&
                            animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"))
                            moveAmount = animator.GetFloat(AnimatorParameters.MoveAmount) / 1.5f;
                    }

                    PlaySfx(sounds[Random.Range(0, sounds.Count)], moveAmount);
                }
                else if (playerController == null)
                    PlaySfx(sounds[Random.Range(0, sounds.Count)]);
            }

            // 파티클 효과 처리
            if (particleEffects is { Count: > 0 })
            {
                if(playerController != null && !particleIgnoreStates.Contains(playerController.FocusedSystemState))
                    SpawnParticle(particleEffects[Random.Range(0, particleEffects.Count)], footTransform);
                else if(playerController == null)
                    SpawnParticle(particleEffects[Random.Range(0, particleEffects.Count)], footTransform);
            }
        }

        // PlaySfx: 주어진 오디오 클립을 재생합니다.
        // volume: 재생 볼륨 (기본값 1)
        void PlaySfx(AudioClip clip, float volume = 1)
        {
            
            volume = Mathf.Max(volume, minVolume);
            
            SoundManager.PlaySFX(clip, transform.position, volume);
        }

        // SpawnParticle: 주어진 파티클 이펙트를 footTransform 위치와 회전값으로 인스턴스화합니다.
        void SpawnParticle(GameObject particleEffect, Transform footTransform)
        {
            var particleObj = Instantiate(particleEffect, footTransform.position, footTransform.rotation);
            Destroy(particleObj, 2f);
        }

        // GetOverrideEffect: FloorStepData를 기반으로 overrideStepEffects 목록에서 재정의 효과를 찾습니다.
        OverrideStepEffects GetOverrideEffect(FloorStepData floorData)
        {
            if (overrideType == StepEffectsOverrideType.MaterialName)
                return overrideStepEffects.FirstOrDefault(x => x.MaterialName == floorData.MaterialName);
            else if (overrideType == StepEffectsOverrideType.TextureName)
                return overrideStepEffects.FirstOrDefault(x => x.TextureName == floorData.TextureName);
            else
                return overrideStepEffects.FirstOrDefault(x => x.Tag == floorData.Tag);
        }

        // IsLayerInLayerMask: 주어진 layer가 layerMask에 포함되어 있는지 여부를 확인합니다.
        bool IsLayerInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }

        // GroundLayer 프로퍼티: groundLayer 값을 외부에서 읽을 수 있도록 합니다.
        public LayerMask GroundLayer => groundLayer;
    }

    // OverrideStepEffects 클래스는 발 착지 효과를 재정의할 때 사용할 설정들을 담고 있습니다.
    [global::System.Serializable]
    public class OverrideStepEffects
    {
        public string tag;                // 태그로 재정의할 경우 사용
        public Material materialName;     // 재질 이름으로 재정의할 경우 사용
        public Texture textureName;       // 텍스처 이름으로 재정의할 경우 사용

        public bool overrideFootStepSounds;    // 소리 재정의 여부
        public bool overrideFootStepParticles;   // 파티클 재정의 여부

        public List<AudioClip> footStepSounds;   // 재정의 발 착지 소리 목록
        public List<GameObject> footStepParticles; // 재정의 발 착지 파티클 목록

        // Tag 프로퍼티: tag 필드를 반환합니다.
        public string Tag => tag;
        // MaterialName 프로퍼티: materialName의 이름을 반환하며, 설정할 수도 있습니다.
        public string MaterialName { 
            get => materialName.name;
            set => materialName.name = value;
        }
        // TextureName 프로퍼티: textureName의 이름을 반환하며, 설정할 수도 있습니다.
        public string TextureName {
            get => textureName.name;
            set => textureName.name = value;
        }
    }

    // StepEffectsOverrideType 열거형: 재정의 기준 타입을 지정합니다.
    public enum StepEffectsOverrideType { MaterialName, TextureName, Tag }
}
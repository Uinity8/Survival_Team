using UnityEngine;

namespace Framework.Characters
{
    public class FootstepAudioController : MonoBehaviour
    {
        [Header("Landing Sound")]
        [SerializeField] private AudioClip LandingAudioClip;
        
        [Header("Footstep Sounds")]
        [SerializeField] private AudioClip[] FootstepAudioClips;
        [Range(0f, 1f)]
        [SerializeField] private float FootstepAudioVolume = 0.5f;

        private CharacterController _controller; // 사용자의 캐릭터 컨트롤러 참조

        private void Awake()
        {
            // 캐릭터 컨트롤러 컴포넌트 가져오기
            _controller = GetComponent<CharacterController>();
        }
        
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center),
                        FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center),
                    FootstepAudioVolume);
            }
        }
    }
}
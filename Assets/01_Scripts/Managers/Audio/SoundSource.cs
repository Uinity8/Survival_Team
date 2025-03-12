using Unity.VisualScripting;
using UnityEngine;

public class SoundSource : MonoBehaviour
{
    private AudioSource _audioSource; // 오디오 소스 컴포넌트

    public void Play(AudioClip clip, float soundEffectVolume, float soundEffectPitchVariance)
    {
        if (_audioSource == null)
            _audioSource = gameObject.GetOrAddComponent<AudioSource>(); // 오디오 소스 가져오기 (초기화)

        CancelInvoke(); // 기존에 예약된 Invoke 취소
        _audioSource.clip = clip; // 재생할 오디오 클립 설정
        _audioSource.volume = soundEffectVolume; // 볼륨 설정
        _audioSource.Play(); // 오디오 재생
        
        // 피치(음 높낮이) 설정 (랜덤 변동 적용)
        _audioSource.pitch = 1f + Random.Range(-soundEffectPitchVariance, soundEffectPitchVariance);

        // 오디오 클립 길이 + 2초 후 자동 비활성화
        Invoke(nameof(Disable), clip.length + 2);
    }

    public void Disable()
    {
        _audioSource.Stop(); // 오디오 정지
        Destroy(this.gameObject); // 오브젝트 삭제
    }
}
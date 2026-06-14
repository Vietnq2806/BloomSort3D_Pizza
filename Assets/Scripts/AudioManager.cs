using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource _audioSource;

    // Kẹp giữ clips nhạc tổng hợp động
    private AudioClip _mergeSynthClip;
    private AudioClip _bloomSynthClip;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        // Sinh âm thanh tổng hợp động (Zero File Dependencies - 100% Chạy ngay lập tức)
        GenerateSynthAudioClips();
    }

    void OnEnable()
    {
        GameEvents.OnSliceMerged += PlayMergeSound;
        GameEvents.OnPlateBloomed += PlayBloomSound;
    }

    void OnDisable()
    {
        GameEvents.OnSliceMerged -= PlayMergeSound;
        GameEvents.OnPlateBloomed -= PlayBloomSound;
    }

    private void PlayMergeSound(Transform slice, Transform target, int combo)
    {
        if (_audioSource == null || _mergeSynthClip == null) return;

        // COMBO PITCH SHIFT: Tăng tiến Pitch theo bán âm âm nhạc (semitones)
        // Công thức: pitch = 1.059463 ^ combo (tương ứng mỗi nốt nhạc trên phím đàn)
        float semitoneRatio = 1.059463f; 
        float newPitch = Mathf.Pow(semitoneRatio, Mathf.Clamp(combo - 1, 0, 12));
        
        _audioSource.pitch = newPitch;
        _audioSource.PlayOneShot(_mergeSynthClip, 0.5f);
    }

    private void PlayBloomSound(Transform plate, string flavor, int combo)
    {
        if (_audioSource == null || _bloomSynthClip == null) return;

        // Cao độ nổ rực rỡ hơn chút so với tiếng gộp thường
        float semitoneRatio = 1.059463f;
        float newPitch = Mathf.Pow(semitoneRatio, Mathf.Clamp(combo + 3, 0, 15));

        _audioSource.pitch = newPitch;
        _audioSource.PlayOneShot(_bloomSynthClip, 0.7f);
    }

    // ==========================================
    // THUẬT TOÁN TỔNG HỢP ÂM THANH SINE WAVE & RESOUNDING CHIME
    // Bằng mã toán học động (Zero GC / Zero asset references)
    // ==========================================
    private void GenerateSynthAudioClips()
    {
        int sampleRate = 44100;

        // 1. Tiếng gộp (Merge Sound): Tiếng Ping bíp ngắn, trong trẻo
        float mergeDuration = 0.15f;
        int mergeLength = (int)(sampleRate * mergeDuration);
        float[] mergeData = new float[mergeLength];
        
        for (int i = 0; i < mergeLength; i++)
        {
            float time = (float)i / sampleRate;
            // Tần số gốc C5 (523.25 Hz)
            float freq = 523.25f;
            // Sóng sin cơ bản kèm tắt tiếng nhanh dần (exponential decay)
            float envelope = Mathf.Exp(-time * 18f);
            mergeData[i] = Mathf.Sin(2f * Mathf.PI * freq * time) * envelope;
        }

        _mergeSynthClip = AudioClip.Create("MergeSynthClip", mergeLength, 1, sampleRate, false);
        _mergeSynthClip.SetData(mergeData, 0);

        // 2. Tiếng nổ (Bloom Sound): Âm sắc rực rỡ vang dội hơn
        float bloomDuration = 0.35f;
        int bloomLength = (int)(sampleRate * bloomDuration);
        float[] bloomData = new float[bloomLength];

        for (int i = 0; i < bloomLength; i++)
        {
            float time = (float)i / sampleRate;
            // Hợp âm gốc G5 (783.99 Hz) trộn với C6 (1046.50 Hz)
            float freq1 = 783.99f;
            float freq2 = 1046.50f;
            
            float envelope = Mathf.Exp(-time * 8f); // Vang lâu hơn chút
            float wave = Mathf.Sin(2f * Mathf.PI * freq1 * time) * 0.6f + Mathf.Sin(2f * Mathf.PI * freq2 * time) * 0.4f;
            
            bloomData[i] = wave * envelope;
        }

        _bloomSynthClip = AudioClip.Create("BloomSynthClip", bloomLength, 1, sampleRate, false);
        _bloomSynthClip.SetData(bloomData, 0);
    }
}

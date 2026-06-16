using UnityEngine;

[CreateAssetMenu(fileName = "AudioSettings", menuName = "Settings/Audio Settings")]
public class AudioSettingsSO : ScriptableObject
{
    [Range(0f, 1f)] public float musicVolume = 1f;
}

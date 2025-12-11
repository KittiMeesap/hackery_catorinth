[System.Serializable]
public class SettingsData
{
    public int displayMode; // 0 = Windowed, 1 = Borderless, 2 = Fullscreen
    public int resolutionIndex;

    public float masterVolume; // 0..1
    public float musicVolume; // 0..1
    public float sfxVolume; // 0..1
}

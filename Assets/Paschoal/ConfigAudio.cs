using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class ConfigAudio : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider sliderFx,sliderMaster,sliderBGM;

    public void FxVolume()
    {
        audioMixer.SetFloat("FxVolume", sliderFx.value);
    }
    public void MusicVolume()
    {
        audioMixer.SetFloat("MusicVolume", sliderBGM.value);
    }
    public void MasterVolume()
    {
        audioMixer.SetFloat("MasterVolume", sliderMaster.value);
    }
    private void OnEnable()
    {
        audioMixer.GetFloat("MasterVolume", out float master);
        audioMixer.GetFloat("MusicVolume", out float music);
        audioMixer.GetFloat("FxVolume", out float sfx);

        sliderFx.value = sfx;
        sliderMaster.value = master;
        sliderBGM.value = music;
    }
}

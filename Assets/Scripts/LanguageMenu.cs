using UnityEngine;
using UnityEngine.Localization.Settings;

public class LanguageMenu : MonoBehaviour
{
    private const string DefaultLanguageCode = "en";

    void Awake()
    {
        if (LocalizationSettings.SelectedLocale == null)
        {
            SetLocale(DefaultLanguageCode);
        }
    }

    public void SetThai() => SetLocale("th");

    public void SetEnglish() => SetLocale("en");

    private async void SetLocale(string code)
    {
        await LocalizationSettings.InitializationOperation.Task;

        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
        }
        else
        {
            Debug.LogWarning($"Locale '{code}' not found in Available Locales.");
        }
    }
}

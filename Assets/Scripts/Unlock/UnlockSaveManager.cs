using System.IO;
using UnityEngine;

public class UnlockSaveManager : MonoBehaviour
{
    public static UnlockSaveManager Instance { get; private set; }

    private string savePath;
    public UnlockSaveData Data { get; private set; }

    private void Awake()
    {
        Instance = this;

        savePath = Path.Combine(Application.persistentDataPath, "unlock_save.json");
        Load();
    }

    public void Load()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            Data = JsonUtility.FromJson<UnlockSaveData>(json);
        }
        else
        {
            Data = new UnlockSaveData();
            Save();
        }
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(savePath, json);
    }

    public void ClearSave()
    {
        Data = new UnlockSaveData();
        Save();
    }
}

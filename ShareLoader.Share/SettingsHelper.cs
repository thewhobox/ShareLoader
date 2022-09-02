namespace ShareLoader.Share;

public class SettingsHelper
{
    public static Dictionary<string, string> Settings { get; set; }

    public static T GetSetting<T>(string name)
    {
        if(Settings == null) Load();
        if(!Settings.ContainsKey(name)) return (T)Convert.ChangeType(null, typeof(T));
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Settings[name]);
    }

    public static string GetSetting(string name)
    {
        if(Settings == null) Load();
        if(!Settings.ContainsKey(name)) return "";
        return Settings[name];
    }

    
    public static void SetSetting(string name, object value)
    {
        string val = Newtonsoft.Json.JsonConvert.SerializeObject(value);
        if(Settings.ContainsKey(name))
            Settings[name] = val;
        else
            Settings.Add(name, val);

        Save();
    }

    public static void SetSetting(string name, string value)
    {
        if(Settings.ContainsKey(name))
            Settings[name] = value;
        else
            Settings.Add(name, value);

        Save();
    }

    public static void Load()
    {
        if(!System.IO.File.Exists("settings.txt"))
        {
            Settings = new Dictionary<string, string>();
            return;
        }
            
        string content = System.IO.File.ReadAllText("settings.txt");
        Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
    }

    public static void Save()
    {
        System.IO.File.WriteAllText("settings.txt", Newtonsoft.Json.JsonConvert.SerializeObject(Settings));
    }
}
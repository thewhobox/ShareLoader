namespace ShareLoader.Share;

public class SettingsHelper
{
    public static string FilePath { get; set; } = "/shareloader/settings.txt";

    public SettingsHelper()
    {
        if(!Directory.Exists("/shareloader/"))
        {
            Console.WriteLine("/shareloader/ doesnt exist");
            FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.txt");
            Console.WriteLine("Using: " + FilePath);
        }
    }

    public static Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

    public static T? GetSetting<T>(string name)
    {
        if(Settings == null) Load();
        if(Settings == null || !Settings.ContainsKey(name)) return (T?)Convert.ChangeType(null, typeof(T?));
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Settings[name]);
    }

    public static string GetSetting(string name)
    {
        if(Settings == null) Load();
        if(Settings == null || !Settings.ContainsKey(name)) return "";
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
        if(!System.IO.File.Exists(FilePath))
        {
            Settings = new Dictionary<string, string>();
            return;
        }
            
        string content = System.IO.File.ReadAllText(FilePath);
        Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(content) ?? new();
    }

    public static void Save()
    {
        System.IO.File.WriteAllText(FilePath, Newtonsoft.Json.JsonConvert.SerializeObject(Settings));
    }
}
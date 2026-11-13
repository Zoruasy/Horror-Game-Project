using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class SaveFile
{
    public const int CurrentFormat = 1;

    public int format = CurrentFormat;
    public string gameVersion;
    public string savedAt;

    public int valuesVersion = ValuesMigrations.CurrentVersion;
    public Dictionary<string, JToken> values = new();

    public Dictionary<string, EntityRecord> entities = new();
}

public class EntityRecord
{
    public bool? active;
    public Dictionary<string, ComponentRecord> components = new();
}

public class ComponentRecord
{
    public int version = 1;
    public JToken data;
}

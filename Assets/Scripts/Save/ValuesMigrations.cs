using Newtonsoft.Json.Linq;

public static class ValuesMigrations
{
    public const int CurrentVersion = 1;

    public static void Migrate(string file, JObject values, int fromVersion)
    {
        // if (fromVersion == 1 && file == "player" && values["scene"] != null)
        // {
        //     values["currentLevel"] = values["scene"];
        //     values.Remove("scene");
        // }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public interface ISaveable
{
    string SaveKey => GetType().Name;

    int StateVersion => 1;

    JToken Migrate(JToken data, int fromVersion) => data;

    JToken CaptureJson();
    void RestoreJson(JToken data);
}

public interface ISaveable<TState> : ISaveable where TState : class
{
    TState CaptureState();
    void RestoreState(TState state);

    JToken ISaveable.CaptureJson() => JToken.FromObject(CaptureState(), SaveSystem.Serializer);

    void ISaveable.RestoreJson(JToken data)
    {
        TState state = CaptureState();
        using (JsonReader reader = data.CreateReader())
            SaveSystem.Serializer.Populate(reader, state);
        RestoreState(state);
    }
}

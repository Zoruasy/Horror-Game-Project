using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public class PlayerRespawn : MonoBehaviour, ISaveable<PlayerRespawn.State>
{
    string spawnScene;
    Vector3 spawnPosition;
    Quaternion spawnRotation = Quaternion.identity;

    public bool HasSpawn => spawnScene == gameObject.scene.name;

    void Awake()
    {
        SetSpawn(transform.position, transform.rotation);
    }

    public bool SetSpawn(Vector3 position, Quaternion rotation)
    {
        rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
        if (HasSpawn && position == spawnPosition && rotation == spawnRotation) return false;

        spawnScene = gameObject.scene.name;
        spawnPosition = position;
        spawnRotation = rotation;
        return true;
    }

    public bool SetSpawn(Transform point) => SetSpawn(point.position, point.rotation);

    public void Respawn()
    {
        if (!HasSpawn) return;
        Teleport(spawnPosition, spawnRotation);
    }

    void Teleport(Vector3 position, Quaternion rotation)
    {
        var controller = GetComponent<UnityEngine.CharacterController>();
        if (controller) controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        if (controller) controller.enabled = true;

        var fpc = GetComponent<CharacterController.FirstPersonController>();
        if (fpc)
        {
            fpc.currentVelocity = Vector3.zero;
            fpc.moveDirection = Vector3.zero;
        }

        //make no noise right after respawning
        var detectionSphere = GetComponentInChildren<CharacterController.DetectionSphere>();
        if (detectionSphere)
            detectionSphere.ForceSilence(detectionSphere.SpawnSilenceDuration);
    }


    // ---- ISaveable ----

    public class State
    {
        public string scene;
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
    }

    public State CaptureState() => new State { scene = spawnScene, position = spawnPosition, rotation = spawnRotation };

    public void RestoreState(State state)
    {
        if (state.scene != gameObject.scene.name) return;

        spawnScene = state.scene;
        spawnPosition = state.position;
        spawnRotation = state.rotation;
        Respawn();
    }
}

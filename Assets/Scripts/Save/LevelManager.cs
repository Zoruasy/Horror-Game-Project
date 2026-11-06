using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SaveSystem.ResetLevel();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            SaveSystem.Save();
        }
    }
}

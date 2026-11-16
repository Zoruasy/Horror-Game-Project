
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelButtonCube : MonoBehaviour
{
    [SerializeField] private string nextScene = "DaanParryScene";

    private void OnMouseDown()
    {
        Debug.Log("Level button pressed!");
        SceneManager.LoadScene(nextScene);
    }
}

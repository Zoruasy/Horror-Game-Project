
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoading : MonoBehaviour
{
    public static LevelLoading Instance;

    [SerializeField] GameObject loadingScreen;
    [SerializeField] GameObject gameUI;
    [SerializeField] GameObject player;
    [SerializeField] GameObject loadingCamera;

    bool isLoading;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        loadingScreen.SetActive(false);
        loadingCamera.SetActive(false);
    }

    public void LoadLevel(string sceneName)
    {
        if (!isLoading)
            StartCoroutine(Load(sceneName));
    }

    IEnumerator Load(string sceneName)
    {
        isLoading = true;

        loadingCamera.SetActive(true);
        loadingScreen.SetActive(true);

        if (gameUI != null) gameUI.SetActive(false);
        if (player != null) player.SetActive(false);

        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(3f);
        yield return SceneManager.LoadSceneAsync(sceneName);

        Time.timeScale = 1f;
        loadingScreen.SetActive(false);
        loadingCamera.SetActive(false);

        isLoading = false;
    }
}

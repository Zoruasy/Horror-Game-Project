
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoading : MonoBehaviour
{
    public static LevelLoading Instance;

    [SerializeField] GameObject loadingScreen;
    [SerializeField] GameObject gameUI;
    [SerializeField] MonoBehaviour[] playerControls;

    bool isLoading;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        loadingScreen.SetActive(false);
    }

    public void LoadLevel(string sceneName)
    {
        if (!isLoading)
            StartCoroutine(Load(sceneName));
    }

    IEnumerator Load(string sceneName)
    {
        isLoading = true;

        if (gameUI != null)
            gameUI.SetActive(false);

        foreach (var control in playerControls)
            if (control != null)
                control.enabled = false;

        Time.timeScale = 0f;
        loadingScreen.SetActive(true);

        yield return new WaitForSecondsRealtime(3f);
        yield return SceneManager.LoadSceneAsync(sceneName);

        loadingScreen.SetActive(false);
        Time.timeScale = 1f;
        isLoading = false;
    }
}

using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreenController : MonoBehaviour
{
    private static LoadingScreenController _instance;

    [Header("UI References")]
    [SerializeField] private GameObject loadingCanvas;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        loadingCanvas.SetActive(false);
    }

    public static void LoadScene(string sceneName)
    {
        if (_instance == null)
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            _instance.LoadSceneInternal(sceneName);
        }
    }

    public static void RemoveLoadScreen() => _instance.RemoveLoadScreenInternal();
    private void RemoveLoadScreenInternal()
    {
        if (progressBar != null) 
            progressBar.value = 1f;

        if (progressText != null) 
            progressText.text = "100";
        
        StartCoroutine(RemoveLoadScreenCoroutine());
    }
    private IEnumerator RemoveLoadScreenCoroutine()
    {            
        yield return new WaitForSecondsRealtime(0.4f);
        _instance.loadingCanvas.SetActive(false);   
    }
    
    private void LoadSceneInternal(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncRoutine(sceneName));
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneName)
    {
        loadingCanvas.SetActive(true);

        var asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        if (asyncLoad == null)
        {
            Debug.LogError("Scene name not found: " + sceneName);
            yield return null;
        }
        
        var fakeProgress = 0f;

        while (!asyncLoad.isDone)
        {
            var targetProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            fakeProgress = Mathf.MoveTowards(fakeProgress, targetProgress, 2f * Time.unscaledDeltaTime);
            
            if (progressBar != null) 
                progressBar.value = fakeProgress;

            if (progressText != null) 
                progressText.text = $"{fakeProgress * 100f:F0}";

            if (asyncLoad.progress >= 0.9f && fakeProgress >= 1f)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}

using UnityEngine;


public abstract class SceneLocalSingleton<T> : MonoBehaviour, ISceneLifecycleDependant where T : SceneLocalSingleton<T>
{
    public static T Instance { get; private set; }

    public void OnSceneLoad()
    {
        if (Instance == null)
        {
            Instance = (T)this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void OnSceneUnload()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
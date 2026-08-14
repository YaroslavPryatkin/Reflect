using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-200)]
public class ScreenFatherController : MonoBehaviour
{
    [SerializeField] private GameObject startingScreen;
    
    private readonly List<GameObject> _screens = new ();
    private int _startingScreen;
    
    private void Awake()
    {
        CacheScreens();
    }

    private void OnEnable()
    {
        ResetToDefaultScreen();
    }

    public void CacheScreens()
    {
        _screens.Clear();

        if (startingScreen)
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject == startingScreen)
                    _startingScreen = _screens.Count;
                
                _screens.Add(child.gameObject);
            }
        }
        else
        {
            foreach (Transform child in transform)
            {
                _screens.Add(child.gameObject);
            }
            _startingScreen = 0;
        }
    }

    public void ResetToDefaultScreen()
    {
        OpenScreen(_startingScreen);
    }


    public void OpenScreen(int index)
    {
        if (index < 0 || index >= _screens.Count)
        {
            Debug.LogWarning($"[MultipleScreenController] Screen with index {index} does not exist!");
            return;
        }

        for (int i = 0; i < _screens.Count; i++)
        {
            _screens[i].SetActive(i == index);
        }
    }

    public int GetScreenIndex(GameObject screen)
    {
        return _screens.IndexOf(screen);
    }

    public bool DoesHaveIndex(int index)
    {
        return index >= 0 && index < _screens.Count;
    }
}

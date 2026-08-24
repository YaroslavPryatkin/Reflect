using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelEnterController : MonoBehaviour
{
    [SerializeField] private bool availableOnStart;
    [SerializeField] private string sceneName;
    
    [Header("Buttons")]
    [SerializeField] private LevelEnterButton enterButton;
    [SerializeField] private LevelResetButton resetButton;
    
    [Header("UI")]
    [SerializeField] private GameObject levelOpen;
    [SerializeField] private GameObject levelClose;
    [SerializeField] private TextMeshProUGUI enterButtonText;

    private void Awake()
    {
        if (sceneName == null || string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[LevelEnterController] Missing scene name");
            return;
        }

        InitializeInternal();
    }

    public void Initialize(in LevelInitializer levelInitializer)
    {
        sceneName = levelInitializer.SceneName;
        availableOnStart = levelInitializer.availableOnStart;
        InitializeInternal();
    }

    private void InitializeInternal()
    {
        enterButton.Initialize(sceneName);        
        resetButton.Initialize(this, sceneName);        

        
        if (enterButtonText != null)
        {
            enterButtonText.text = sceneName;
        }
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        var myLevelSave = GameSavings.GetOrCreate(sceneName, availableOnStart);
        enterButton.SetInteractionState(myLevelSave.IsAvailable);
        resetButton.SetInteractionState(myLevelSave.IsAvailable && myLevelSave.CurrentArenaIndex != -1);
        levelOpen.SetActive(myLevelSave.IsAvailable);
        levelClose.SetActive(!myLevelSave.IsAvailable);
    }
}
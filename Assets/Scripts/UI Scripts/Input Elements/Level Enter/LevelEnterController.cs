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
    [SerializeField] private Image levelOpenCloseFieldImage;
    [SerializeField] private GameObject levelOpen;
    [SerializeField] private GameObject levelClose;
    [SerializeField] private TextMeshProUGUI enterButtonText;

    [Header("Level finished color")] 
    [SerializeField] private Color levelFinishedColor = Color.lawnGreen;

    private Image _enterImage;
    private Image _resetImage;
    
    private void Awake()
    {
        if (sceneName == null || string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[LevelEnterController] Missing scene name");
            return;
        }
        
        _enterImage = enterButton.GetComponent<Image>();
        _resetImage = resetButton.GetComponent<Image>();
        
        SetImageColor(Color.white);
        
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
        
        SetImageColor(myLevelSave.IsFinished ?  levelFinishedColor : Color.white);
    }

    private void SetImageColor(Color color)
    {
        _enterImage.color = color;
        _resetImage.color = color;
        levelOpenCloseFieldImage.color = color;
    }
}
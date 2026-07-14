using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-200)]
public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance;

    [SerializeField] private GameObject UI;
    [SerializeField] private GameObject escapeMenu;
    [SerializeField] private GameObject inGameUI;

    [SerializeField] private string playerInputMapName = "Player";
    [SerializeField] private string uiInputMapName = "UI";
    public EscapeMenuController EscapeMenuController { get; private set; }
    
    public bool IsGameActive { get;private set; } = false;
    
    
    private PlayerInput _playerInput;
    private GlobalGameInputManager _globalGameInputManager;

    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        EscapeMenuController = escapeMenu.GetComponent<EscapeMenuController>();
        
        EscapeMenuController.ContinueButton.onClick.AddListener(ShowGame);
        EscapeMenuController.ExitButton.onClick.AddListener(CloseGame);
        
        _playerInput = GetComponent<PlayerInput>();
        _globalGameInputManager = GetComponent<GlobalGameInputManager>();
    }

    private void OnEnable()
    {
        _globalGameInputManager.OnPauseEvent += ShowEscapeMenu;
    }

    private void OnDisable()
    {
        _globalGameInputManager.OnPauseEvent -= ShowEscapeMenu;
    }

    private void Start()
    {
        UI.SetActive(true);
        _playerInput.SwitchCurrentActionMap(playerInputMapName);//to immediately disable it
        ShowEscapeMenu();
    }

    public void ShowGame()
    {
        inGameUI.SetActive(true);
        escapeMenu.SetActive(false);
        IsGameActive = true;
        SwitchToPlayerInputMap();
        LockCursor();
        GlobalTimeScaleController.ReturnTimePace(this);
    }

    public void ShowEscapeMenu()
    {
        inGameUI.SetActive(false);
        escapeMenu.SetActive(true);
        IsGameActive = false;
        SwitchToUIInputMap();
        UnlockCursor();
        GlobalTimeScaleController.ChangeTimePace(this,0);
    }

    public void CloseGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    
    private static void LockCursor(){
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    private static void UnlockCursor(){
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void SwitchToPlayerInputMap()
    {
        _playerInput.currentActionMap.Disable();
        _playerInput.SwitchCurrentActionMap(playerInputMapName);
        _playerInput.currentActionMap.Enable();
    }

    private void SwitchToUIInputMap()
    {
        _playerInput.currentActionMap.Disable();
        _playerInput.SwitchCurrentActionMap(uiInputMapName);
        _playerInput.currentActionMap.Enable();
    }
    
}

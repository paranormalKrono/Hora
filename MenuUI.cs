using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private float _horaCenterTime = 5f;
    [SerializeField] private float _horaUpTime = 2f;
    [SerializeField] private float _timeToPause = 2f;
    [SerializeField] private float _defeatTime = 4f;

    private VisualElement _mainMenu;
    private VisualElement _panelHora;
    private VisualElement _containerHora;
    private VisualElement _settingsMenu;
    private VisualElement _actionsMenu;
    private VisualElement _storyMenu;
    private VisualElement _defeatMenu;

    private Button _buttonPlay;
    private Button _buttonSettings;
    private Button _buttonExit;
    private Button _buttonSettingsClose;

    private Label _horaTimer;
    private Label _horaTimerHuman;
    private Label _horaTimerRed;

    public UnityAction _OnHoraCenter;
    public UnityAction _OnHoraUp;
    public UnityAction _OnMainMenu;
    public UnityAction _OnGameMenu;
    public UnityAction _OnSwitchMenuToSettings;
    public UnityAction _OnSwitchSettingsToMenu;
    public UnityAction _OnSwitchMenuToGame;
    public UnityAction _OnSwitchGameToMenu;
    public UnityAction _OnTimer;

    private IEnumerator _ITimer;
    private IEnumerator _ITimerChange;

    private enum MenuState
    {
        MainMenu,
        Settings,
        Game,
        Unknown
    }

    private MenuState _state = MenuState.MainMenu;
    private bool isFreshStart = true;

    public float _currentTimerValue = 0f;
    public bool _isTimer = false;
    public bool _isTimerChange = false;

    public float CurrentTimerValue { get => _currentTimerValue; }
    public bool IsTimer { get => _isTimer; }

    private void Awake()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        _mainMenu = root.Q<VisualElement>("PanelMenu");
        _panelHora = root.Q<VisualElement>("PanelHora");
        _containerHora = root.Q<VisualElement>("ContainerHora");
        _settingsMenu = root.Q<VisualElement>("PanelSettings");
        _actionsMenu = root.Q<VisualElement>("PanelActions");
        _storyMenu = root.Q<VisualElement>("PanelStory");
        _defeatMenu = root.Q<VisualElement>("PanelDefeat");

        _buttonPlay = root.Q<Button>("ButtonPlay");
        _buttonPlay.RegisterCallback<ClickEvent>(OnPlayButtonClicked);
        _buttonExit = root.Q<Button>("ButtonExit");
        _buttonSettings = root.Q<Button>("ButtonSettings");
        _buttonSettings.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
        _buttonSettingsClose = root.Q<Button>("ButtonSettingsClose");
        _buttonSettingsClose.RegisterCallback<ClickEvent>(OnSettingsCloseButtonClicked);

        _horaTimer = root.Q<Label>("HoraTimer");
        _horaTimerHuman = root.Q<Label>("HoraTimerHuman");
        _horaTimerRed = root.Q<Label>("HoraTimerRed");

        DisableSettings();
    }

    public void HoraUp()
    {
        StartCoroutine(IHoraUp());
    }

    public void ChangeTimer(float value, float hum, float red)
    {
        if (_isTimerChange)
        {
            _isTimerChange = false;
            _horaTimerHuman.AddToClassList("hora_timer_human_hide");
            _horaTimerRed.AddToClassList("hora_timer_human_hide");
            StopCoroutine(_ITimerChange);
        }
        _horaTimerHuman.text = ((int)hum).ToString();
        _horaTimerRed.text = "+" + ((int)red).ToString();
        _ITimerChange = ITimerChange();
        StartCoroutine(_ITimerChange);
        ChangeTimer(value);
    }

    public void ChangeTimer(float value)
    {
        _currentTimerValue += value;
        SetUITimerValue();
    }

    public void TimerSetValue(float value) {
        _currentTimerValue = value;
        SetUITimerValue();
    }

    public void StartTimer(float target_value, float speed)
    {
        if (_isTimer)
        {
            _isTimer = false;
            StopCoroutine(_ITimer);
        }
        _ITimer = ITimer(target_value, speed);
        StartCoroutine(_ITimer);
    }

    public void StopTimers() {
        if (_isTimer)
        {
            _isTimer = false;
            StopCoroutine(_ITimer);
        }
        if (_isTimerChange)
        {
            _isTimerChange = false;
            _horaTimerHuman.AddToClassList("hora_timer_human_hide");
            _horaTimerRed.AddToClassList("hora_timer_human_hide");
            StopCoroutine(_ITimerChange);
        }
    }

    public void ToMainMenu()
    {
        StartCoroutine(IToMainMenu());
    }

    public void ToGameMenu()
    {
        StartCoroutine(IToGameMenu());
    }

    public void ShowDefeat()
    {
        StartCoroutine(IDefeat());
    }

    private void OnPlayButtonClicked(ClickEvent clickEvent)
    {
        Debug.Log("Play clicked");
        if (_state == MenuState.MainMenu)
        {
            if (isFreshStart)
            {
                isFreshStart = false;
                StartCoroutine(IHoraCenter());
            }
            else
            {
                _panelHora.AddToClassList("panel_hora_center");
                DisableMainMenu();
                HoraUp();
            }
        }
    }

    private IEnumerator IHoraCenter()
    {
        _storyMenu.RemoveFromClassList("panel_story_hide");
        _panelHora.AddToClassList("panel_hora_center"); 
        DisableMainMenu();
        yield return new WaitForSeconds(_horaCenterTime);
        _storyMenu.AddToClassList("panel_story_hide");
        _OnHoraCenter();
    }

    private void OnSettingsButtonClicked(ClickEvent clickEvent)
    {
        Debug.Log("Settings clicked");
        if (_state == MenuState.MainMenu)
        {
            _OnSwitchMenuToSettings();
            DisableMainMenu();
            EnableSettings();
        }
    }

    private void OnSettingsCloseButtonClicked(ClickEvent clickEvent)
    {
        Debug.Log("Settings close clicked");
        if (_state == MenuState.Settings)
        {
            _OnSwitchSettingsToMenu();
            EnableMainMenu();
            DisableSettings();
        }
    }

    private void DisableSettings()
    {
        _settingsMenu.AddToClassList("settings_hide");
        _buttonSettingsClose.SetEnabled(false);
    }

    private void EnableSettings()
    {
        _state = MenuState.Settings;
        _settingsMenu.RemoveFromClassList("settings_hide");
        _buttonSettingsClose.SetEnabled(true);
    }

    private void DisableMainMenu()
    {
        _mainMenu.AddToClassList("main_menu_hide");
        _buttonPlay.SetEnabled(false);
        _buttonSettings.SetEnabled(false);
        _buttonExit.SetEnabled(false);
    }
    private void EnableMainMenu()
    {
        _state = MenuState.MainMenu;
        _mainMenu.RemoveFromClassList("main_menu_hide");
        _buttonPlay.SetEnabled(true);
        _buttonSettings.SetEnabled(true);
        _buttonExit.SetEnabled(true);
    }

    private IEnumerator IHoraUp()
    {
        _OnSwitchMenuToGame();
        _containerHora.AddToClassList("container_hora_up");
        yield return new WaitForSeconds(_horaUpTime);
        _actionsMenu.RemoveFromClassList("actions_hide");
        _state = MenuState.Game;
        _OnHoraUp();
    }

    private IEnumerator IToMainMenu()
    {
        _OnSwitchGameToMenu();
        _state = MenuState.Unknown;
        _panelHora.RemoveFromClassList("panel_hora_center");
        _containerHora.RemoveFromClassList("container_hora_up");
        _actionsMenu.AddToClassList("actions_hide");
        yield return new WaitForSeconds(_timeToPause);
        EnableMainMenu();
        _state = MenuState.MainMenu;
        _OnMainMenu();
    }

    private IEnumerator IToGameMenu()
    {
        _OnSwitchMenuToGame();
        _state = MenuState.Unknown;
        _panelHora.AddToClassList("panel_hora_up");
        _containerHora.AddToClassList("container_hora_up");
        DisableMainMenu();
        yield return new WaitForSeconds(_timeToPause);
        _actionsMenu.RemoveFromClassList("actions_hide");
        _OnGameMenu();
        _state = MenuState.Game;
    }

    private IEnumerator IDefeat()
    {
        isFreshStart = true;
        _defeatMenu.RemoveFromClassList("panel_defeat_hide");
        yield return new WaitForSeconds(_defeatTime);
        _defeatMenu.AddToClassList("panel_defeat_hide");
        ToMainMenu();
    }

    private void SetUITimerValue()
    {
        _horaTimer.text = "00:00:" + TimeSpan.FromSeconds(_currentTimerValue).ToString(@"dd\:hh\:mm\:ss");// $"{ts.Days}:{ts.Hours}:{ts.Minutes}:{ts.Seconds}";
    }

    private IEnumerator ITimer(float target_value, float speed)
    {
        _isTimer = true;
        float diff = target_value - _currentTimerValue;
        float change = speed * math.sign(diff);
        while (math.abs(_currentTimerValue - target_value) > 1f)
        {
            float delta = Time.deltaTime;
            _currentTimerValue += delta * change;
            SetUITimerValue();
            yield return null;
        }
        if (_currentTimerValue < 0f)
        {
            _currentTimerValue = 0f;
            SetUITimerValue();
        }
        _isTimer = false;
        _OnTimer();
    }

    private IEnumerator ITimerChange()
    {
        _isTimerChange = true;
        _horaTimerHuman.RemoveFromClassList("hora_timer_human_hide");
        _horaTimerRed.RemoveFromClassList("hora_timer_human_hide");
        float t = 3f;
        while (t > 0)
        {
            t -= Time.deltaTime;
            yield return null;
        }
        _horaTimerHuman.AddToClassList("hora_timer_human_hide");
        _horaTimerRed.AddToClassList("hora_timer_human_hide");
        _isTimerChange = false;
    }
}

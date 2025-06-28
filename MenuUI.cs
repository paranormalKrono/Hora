using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private UIDocument UIDocument;
    [SerializeField] private AudioMixer audioMixer;

    private VisualElement _panelMain;
    private VisualElement _panelTimerHora;
    private VisualElement _panelTimerGlobal;
    private VisualElement _panelSettings;
    private VisualElement _panelTitles;
    private VisualElement _panelActions;
    private VisualElement _panelStory;
    private VisualElement _panelTutorHuman;
    private VisualElement _panelTutorRed;
    private VisualElement _panelDefeat;
    private VisualElement _panelContinue;

    private Button _buttonPlay;
    private Button _buttonSettings;
    private Button _buttonSettingsClose;
    private Button _buttonTitles;
    private Button _buttonTitlesClose;
    private Button _buttonExit;

    private Label _labelTimerHora;
    private Label _labelTimerGlobal;
    private Label _labelScoreHuman;
    private Label _labelScoreRed;
    private Label _labelScoreTotal;

    private DropdownField _settingsTranslation;

    private DateTime _timerGlobalDate;

    public enum Menu
    {
        Main,
        Settings,
        Titles,
        Story,
        TutorHuman,
        TutorRed,
        TimerCenter,
        TimerUp,
        Actions,
        Defeat,
        Score,
        Continue,
        TimerGlobal,
    }

    public UnityAction<ClickEvent> _OnPlay;
    public UnityAction<ClickEvent> _OnSettings;
    public UnityAction<ClickEvent> _OnSettingsClose;
    public UnityAction<ClickEvent> _OnTitles;
    public UnityAction<ClickEvent> _OnTitlesClose;
    public UnityAction<ClickEvent> _OnExit;


    private void Awake()
    {
        VisualElement root = UIDocument.rootVisualElement;

        _panelMain = root.Q<VisualElement>("PanelMenu");
        _panelTimerHora = root.Q<VisualElement>("PanelTimerHora");
        _panelTimerGlobal = root.Q<VisualElement>("PanelTimerGlobal");
        _panelSettings = root.Q<VisualElement>("PanelSettings");
        _panelTitles = root.Q<VisualElement>("PanelTitles");
        _panelActions = root.Q<VisualElement>("PanelActions");
        _panelStory = root.Q<VisualElement>("PanelStory");
        _panelTutorHuman = root.Q<VisualElement>("PanelTutorHuman");
        _panelTutorRed = root.Q<VisualElement>("PanelTutorRed");
        _panelDefeat = root.Q<VisualElement>("PanelDefeat");
        _panelContinue = root.Q<VisualElement>("PanelContinue");


        _labelTimerHora = root.Q<Label>("LabelTimerHora");
        _labelTimerGlobal = root.Q<Label>("LabelTimerGlobal");
        _labelScoreHuman = root.Q<Label>("HoraScoreHuman");
        _labelScoreRed = root.Q<Label>("HoraScoreRed");
        _labelScoreTotal = root.Q<Label>("HoraScoreTotal");


        _buttonPlay = root.Q<Button>("ButtonPlay");
        _buttonSettings = root.Q<Button>("ButtonSettings");
        _buttonSettingsClose = root.Q<Button>("ButtonSettingsClose");
        _buttonTitles = root.Q<Button>("ButtonTitles");
        _buttonTitlesClose = root.Q<Button>("ButtonTitlesClose");
        _buttonExit = root.Q<Button>("ButtonExit");


        _buttonPlay.RegisterCallback((ClickEvent clickEvent) => _OnPlay?.Invoke(clickEvent));
        _buttonSettings.RegisterCallback((ClickEvent clickEvent) => _OnSettings?.Invoke(clickEvent));
        _buttonSettingsClose.RegisterCallback((ClickEvent clickEvent) => _OnSettingsClose?.Invoke(clickEvent));
        _buttonTitles.RegisterCallback((ClickEvent clickEvent) => _OnTitles?.Invoke(clickEvent));
        _buttonTitlesClose.RegisterCallback((ClickEvent clickEvent) => _OnTitlesClose?.Invoke(clickEvent));

        _settingsTranslation = root.Q<DropdownField>("DropdownTranslation");
        _settingsTranslation.RegisterValueChangedCallback((evt) =>
        {
            LoadTranslation(evt.newValue);
        });
        LoadTranslation("EN");

        Slider sliderMusic = root.Q<Slider>("SliderMusic");
        sliderMusic.RegisterValueChangedCallback((evt) =>
        {
            audioMixer.SetFloat("Music", evt.newValue);
        });

        Slider sliderEffects = root.Q<Slider>("SliderEffects");
        sliderEffects.RegisterValueChangedCallback((evt) =>
        {
            audioMixer.SetFloat("Effects", evt.newValue);
        });
    }

    public void TimerHoraScoreChange(float scoreHuman, float scoreRed)
    {
        _labelScoreHuman.text = "-" + ((int)scoreHuman).ToString();
        _labelScoreRed.text = "+" + ((int)scoreRed).ToString();
        int total = (int)(scoreRed - scoreHuman);
        _labelScoreTotal.text = total > 0 ? "+" + total : total.ToString();
    }

    public void TimerHoraSet(int value)
    {
        // I thought animation can be here, something different from label.
        // $"{ts.Days}:{ts.Hours}:{ts.Minutes}:{ts.Seconds}";
        _labelTimerHora.text = "00:00:" + TimeSpan.FromSeconds(value).ToString(@"dd\:hh\:mm\:ss");
    }

    public void TimerGlobalSet(DateTime value)
    {
        _timerGlobalDate = value;
    }

    public void TimerGlobalAddOneDay()
    {
        _timerGlobalDate = _timerGlobalDate.AddDays(1);
        _labelTimerGlobal.text = _timerGlobalDate.ToString("dd.MM.yyyy");
    }

    public void OpenMenu(Menu menu)
    {
        switch (menu)
        {
            case Menu.Main:
                _panelMain.RemoveFromClassList("hideMain");
                break;
            case Menu.Settings:
                _panelSettings.RemoveFromClassList("hideSettings");
                break;
            case Menu.Story:
                _panelStory.RemoveFromClassList("hideStory");
                _panelTutorHuman.RemoveFromClassList("hideTutorHuman");
                _panelTutorRed.RemoveFromClassList("hideTutorRed");
                break;
            case Menu.TimerCenter:
                _panelTimerHora.AddToClassList("centerTimer");
                break;
            case Menu.TimerUp:
                _panelTimerHora.AddToClassList("upTimer");
                break;
            case Menu.TimerGlobal:
                _panelTimerGlobal.RemoveFromClassList("hideGlobalTime");
                break;
            case Menu.Actions:
                _panelActions.RemoveFromClassList("hideActions");
                break;
            case Menu.Defeat:
                _panelDefeat.RemoveFromClassList("hideDefeat");
                break;
            case Menu.Score:
                _labelScoreHuman.RemoveFromClassList("hideScore");
                _labelScoreRed.RemoveFromClassList("hideScore");
                _labelScoreTotal.RemoveFromClassList("hideScore");
                break;
            case Menu.Continue:
                _panelContinue.RemoveFromClassList("hideContinue");
                break;
            case Menu.Titles:
                _panelTitles.RemoveFromClassList("hideTitles");
                break;
        }
    }

    public void CloseMenu(Menu menu)
    {
        switch (menu)
        {
            case Menu.Main:
                _panelMain.AddToClassList("hideMain");
                break;
            case Menu.Settings:
                _panelSettings.AddToClassList("hideSettings");
                break;
            case Menu.Story:
                _panelStory.AddToClassList("hideStory");
                _panelTutorHuman.AddToClassList("hideTutorHuman");
                _panelTutorRed.AddToClassList("hideTutorRed");
                break;
            case Menu.TimerCenter:
                _panelTimerHora.RemoveFromClassList("centerTimer");
                break;
            case Menu.TimerUp:
                _panelTimerHora.RemoveFromClassList("upTimer");
                break;
            case Menu.TimerGlobal:
                _panelTimerGlobal.AddToClassList("hideGlobalTime");
                break;
            case Menu.Actions:
                _panelActions.AddToClassList("hideActions");
                break;
            case Menu.Defeat:
                _panelDefeat.AddToClassList("hideDefeat");
                break;
            case Menu.Score:
                _labelScoreHuman.AddToClassList("hideScore");
                _labelScoreRed.AddToClassList("hideScore");
                _labelScoreTotal.AddToClassList("hideScore");
                break;
            case Menu.Continue:
                _panelContinue.AddToClassList("hideContinue");
                break;
            case Menu.Titles:
                _panelTitles.AddToClassList("hideTitles");
                break;
        }
    }


    private void LoadTranslation(string id)
    {
        VisualElement root = UIDocument.rootVisualElement;
        Translation tr = new Translation();
        tr.LoadUITranslation(id);
        _settingsTranslation.choices = tr._available;
        tr.TranslateUIToCurrent(root);
    }
}

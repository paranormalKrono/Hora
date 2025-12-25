
using NWorld;
using System;
using System.Collections;
using UnityEngine;
using static BombCreator;
using static MenuUI;

public class Main : MonoBehaviour
{
    [SerializeField] private Input _input;
    [SerializeField] private CameraMover _cameraMover;
    [SerializeField] private MenuUI _ui;
    [SerializeField] private Mapper _mapper;
    [SerializeField] private HoraTimer _timer;
    [SerializeField] private BombCreator _bombCreator;

    [SerializeField] private GameObject _locationMain;
    [SerializeField] private GameObject _locationSettings;
    [SerializeField] private GameObject _locationTitles;
    [SerializeField] private GameObject _locationGame;

    [SerializeField] private Transform _cameraPlaceMenu;
    [SerializeField] private Transform _cameraPlaceSettings;
    [SerializeField] private Transform _cameraPlaceTitles;
    [SerializeField] private Transform _cameraPlaceGame;

    [SerializeField] private DateTime _timerGlobalStartTime = new DateTime(1984, 4, 14);

    [SerializeField] private float _timerStartSpeed = 4f;
    [SerializeField] private float _waitToStory = 3f;
    [SerializeField] private float _waitToFreshStart = 3f;
    [SerializeField] private float _waitScore = 2f;
    

    // COnfiguration
    [SerializeField] private int _timerStartValue = 89;
    [SerializeField] private float humanCoef = 1f;
    [SerializeField] private float redCoef = 1f;

    private enum State
    {
        Menu,
        Game,
    }
    private State _state = State.Menu;
    private bool _isScore = false;
    private IEnumerator _IScore;

    private void Awake()
    {
        _ui.OnExit = (_) => Application.Quit();

        _ui.OnSettings = (_) =>
        {
            _ui.CloseMenu(Menu.Main);
            _ui.OpenMenu(Menu.Settings);
            _locationTitles.SetActive(false);
            _locationSettings.SetActive(true);
            _cameraMover.MoveCameraTo(target: _cameraPlaceSettings, OnMoved: () => _locationMain.SetActive(false));
        };
        _ui.OnSettingsClose = (_) =>
        {
            _ui.OpenMenu(Menu.Main);
            _ui.CloseMenu(Menu.Settings);
            _locationMain.SetActive(true);
            _cameraMover.MoveCameraTo(target: _cameraPlaceMenu, OnMoved: () => _locationSettings.SetActive(false));
        };
        _ui.OnTitles = (_) =>
        {
            _ui.CloseMenu(Menu.Main);
            _ui.OpenMenu(Menu.Titles);
            _locationSettings.SetActive(false);
            _locationTitles.SetActive(true);
            _cameraMover.MoveCameraTo(target: _cameraPlaceTitles, OnMoved: () => _locationMain.SetActive(false)); 
        };
        _ui.OnTitlesClose = (_) =>
        {
            _ui.OpenMenu(Menu.Main);
            _ui.CloseMenu(Menu.Titles);
            _locationMain.SetActive(true);
            _cameraMover.MoveCameraTo(target: _cameraPlaceMenu, OnMoved: () => _locationTitles.SetActive(false)); 
        };

        _input.IsWorking = false;
        _timer.OnTimerValueChanged = (_) => _ui.TimerHoraSet((int)_timer.Value);
        _timer.OnTimerValueChangedOne = () => 
        {
            _ui.TimerGlobalAddOneDay();
            _ui.TimerHoraSet((int)_timer.Value);
        };

        _ui.OnPlay = (_) => PlayFresh();

        _locationGame.SetActive(false);
    }

    private void PlayFresh()
    {
        if (_state == State.Menu)
        {
            _ui.OnPlay = null;
            _mapper.Unbuild();

            _ui.TimerGlobalSet(_timerGlobalStartTime);
            _state = State.Game;
            StartCoroutine(IFresh());
        }
    }

    private void PlayContinue()
    {
        _ui.OnPlay = null;
        _ui.CloseMenu(Menu.Main);
        _ui.OpenMenu(Menu.TimerUp);
        _ui.OpenMenu(Menu.TimerGlobal);
        _locationGame.SetActive(true);
        _mapper.SetState(Mapper.State.Game);
        _cameraMover.MoveCameraTo(_cameraPlaceGame, SetGame);
    }

    private IEnumerator IFresh()
    {
        _ui.CloseMenu(Menu.Main);
        _ui.OpenMenu(Menu.TimerCenter);
        _ui.OpenMenu(Menu.Story);
        yield return new WaitForSeconds(_waitToStory);
        _ui.OpenMenu(Menu.Continue);
        _input.IsWorking = true;
        _input.OnInteract = () =>
        {
            _input.OnInteract = null;
            _input.IsWorking = false;
            _ui.CloseMenu(Menu.Continue);
            _ui.CloseMenu(Menu.Story);
            _timer.StartTimer(target_value: _timerStartValue, speed: _timerStartSpeed, OnTimerEnd: () => StartCoroutine(IFresh2()));
        };
    }

    private IEnumerator IFresh2()
    {
        yield return new WaitForSeconds(_waitToFreshStart);
        _ui.CloseMenu(Menu.TimerCenter);
        _ui.OpenMenu(Menu.TimerUp);
        _ui.OpenMenu(Menu.TimerGlobal);
        _mapper.Build();
        _mapper.SetState(Mapper.State.Game);
        _locationGame.SetActive(true);
        _cameraMover.MoveCameraTo(target: _cameraPlaceGame, OnMoved: SetGame);
    }

    private void SetGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _locationMain.SetActive(false);
        _locationSettings.SetActive(false);
        _locationTitles.SetActive(false);
        _input.OnMove = (Vector2 moveDirection) => _cameraMover.CameraSphereLook(moveDirection);
        _input.OnLook = (Vector2 lookDirection) => _bombCreator.MoveBomb(lookDirection);
        _input.OnScroll = (float scrollWheel) => _bombCreator.MoveBombScroll(scrollWheel);
        _input.OnAttack1 = () =>
        {
            if (_bombCreator.IsBombCreated)
            {
                ExplosionData data = _bombCreator.Explode();
                WorldExplosionData explosionData = _mapper.Explosion(data.position, data.radius);
                Score(explosionData.whiteValue, explosionData.redValue);
            }
            else
            {
                _bombCreator.CreateBomb();
            }
        };
        _input.OnAttack2 = _bombCreator.RemoveBomb;
        _input.OnInteract = () =>
        { // Pause game
            Cursor.lockState = CursorLockMode.None;
            UnsetGameInput();
            _timer.Stop();
            _mapper.SetState(Mapper.State.Paused);
            _state = State.Menu;
            _ui.CloseMenu(Menu.TimerUp);
            _ui.CloseMenu(Menu.TimerGlobal);
            _ui.OpenMenu(Menu.Main);
            _locationMain.SetActive(true);
            _locationGame.SetActive(false);
            _cameraMover.MoveCameraTo(target: _cameraPlaceMenu);
            _ui.OnPlay = (_) => PlayContinue();
        };
        _input.IsWorking = true;
        _timer.StartTimer(target_value: 0f, speed: 1f, OnTimerEnd: () =>
        { // Defeat
            UnsetGameInput();
            _mapper.SetState(Mapper.State.Paused);
            _ui.OpenMenu(Menu.Defeat);
            _ui.OpenMenu(Menu.Continue);
            _input.OnInteract = () => 
            { // Return to menu
                Cursor.lockState = CursorLockMode.None;
                _input.OnInteract = null;
                _state = State.Menu;
                _ui.OpenMenu(Menu.Main);
                _locationMain.SetActive(true);
                _ui.CloseMenu(Menu.TimerUp);
                _ui.CloseMenu(Menu.TimerGlobal);
                _ui.CloseMenu(Menu.Defeat);
                _ui.CloseMenu(Menu.Continue);
                _cameraMover.MoveCameraTo(target: _cameraPlaceMenu, OnMoved: () => 
                { // Returned to menu
                    _locationGame.SetActive(false);
                    _mapper.Unbuild();
                });
                _ui.OnPlay = (_) => PlayFresh();
            };
        });
    }

    private void Score(float whiteValue, float redValue)
    {
        if (_isScore)
        {
            _isScore = false;
            StopCoroutine(_IScore);
        }
        else
        {
            _ui.OpenMenu(Menu.Score);
        }
        _IScore = IScore(whiteValue, redValue);
        StartCoroutine(_IScore);
    }

    private IEnumerator IScore(float whiteValue, float redValue)
    {
        _isScore = true;
        float humScore = humanCoef * whiteValue;
        float redScore = redCoef * redValue;
        _timer.Change(redScore - humScore);
        _ui.TimerHoraScoreChange(scoreHuman: humScore, scoreRed: redScore);
        yield return new WaitForSeconds(_waitScore);
        _ui.CloseMenu(Menu.Score);
        _isScore = false;
    }


    private void UnsetGameInput()
    {
        _input.OnMove = null;
        _input.OnLook = null;
        _input.OnAttack1 = null;
        _input.OnAttack2 = null;
        _input.OnInteract = null;
        _input.OnScroll = null;
    }
}
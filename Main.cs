using NamespaceWorld;
using System;
using System.Collections;
using UnityEngine;
using static MenuUI;

public class Main : MonoBehaviour
{
    [SerializeField] private Input _input;
    [SerializeField] private CameraMover _cameraMover;
    [SerializeField] private CursorSelector _cursorTowerSelector;
    [SerializeField] private MenuUI _ui;
    [SerializeField] private Mapper _mapper;
    [SerializeField] private HoraTimer _timer;

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
        _ui._OnExit = (_) => Application.Quit();

        _ui._OnSettings = (_) =>
        {
            _ui.CloseMenu(Menu.Main);
            _ui.OpenMenu(Menu.Settings);
            _locationTitles.SetActive(false);
            _locationSettings.SetActive(true);
            _cameraMover.MoveCameraTo(target: _cameraPlaceSettings, OnMoved: () => _locationMain.SetActive(false));
        };
        _ui._OnSettingsClose = (_) =>
        {
            _ui.OpenMenu(Menu.Main);
            _ui.CloseMenu(Menu.Settings);
            _locationMain.SetActive(true);
            _cameraMover.MoveCameraTo(target: _cameraPlaceMenu, OnMoved: () => _locationSettings.SetActive(false));
        };
        _ui._OnTitles = (_) =>
        {
            _ui.CloseMenu(Menu.Main);
            _ui.OpenMenu(Menu.Titles);
            _locationSettings.SetActive(false);
            _locationTitles.SetActive(true);
            _cameraMover.MoveCameraTo(target: _cameraPlaceTitles, OnMoved: () => _locationMain.SetActive(false)); 
        };
        _ui._OnTitlesClose = (_) =>
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

        _ui._OnPlay = (_) => PlayFresh();

        _locationGame.SetActive(false);
    }

    private void PlayFresh()
    {
        if (_state == State.Menu)
        {
            _ui._OnPlay = null;
            _mapper.Unbuild();

            _ui.TimerGlobalSet(_timerGlobalStartTime);
            _state = State.Game;
            StartCoroutine(IFresh());
        }
    }

    private void PlayContinue()
    {
        _ui._OnPlay = null;
        _ui.CloseMenu(Menu.Main);
        _ui.OpenMenu(Menu.TimerUp);
        _ui.OpenMenu(Menu.TimerGlobal);
        _locationGame.SetActive(true);
        _mapper.SetGame(true);
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
        _mapper.Build((uint)UnityEngine.Random.Range(0, int.MaxValue));
        _mapper.SetGame(true);
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
        WorldTowers towers = _mapper.world._towers;
        _input.OnLook = (Vector2 lookDirection) => towers.TowerSelectedMove(lookDirection);
        _input.OnScroll = (float scrollWheel) => towers.TowerSelectedMoveScroll(scrollWheel);
        _input.OnAttack1 = () =>
        { // Check cursor and place or grab tower
            if (_cursorTowerSelector.TrySelectRaycast(out Transform tower)) 
            {
                towers.TowerSelect(tower);
            }
            else
            {
                if (towers.State == WorldTowers.SelectionState.Nothing)
                {
                    towers.TowerCreateSelect(NamespaceFactoryTowers.TowerType.Center);
                }
                else
                {
                    towers.TowerSelectedPlace();
                }
            }
        };
        _input.OnAttack2 = () => towers.TowerSelectedDestroy();
        _input.OnInteract = () =>
        { // Pause game
            Cursor.lockState = CursorLockMode.None;
            UnsetGameInput();
            _timer.Stop();
            _mapper.SetGame(false);
            _state = State.Menu;
            _ui.CloseMenu(Menu.TimerUp);
            _ui.CloseMenu(Menu.TimerGlobal);
            _ui.OpenMenu(Menu.Main);
            _locationMain.SetActive(true);
            _locationGame.SetActive(false);
            _cameraMover.MoveCameraTo(target: _cameraPlaceMenu);
            _ui._OnPlay = (_) => PlayContinue();
        };
        _input.IsWorking = true;
        _timer.StartTimer(target_value: 0f, speed: 1f, OnTimerEnd: () =>
        { // Defeat
            UnsetGameInput();
            _mapper._isGame = false;
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
                _ui._OnPlay = (_) => PlayFresh();
            };
        });
    }

    // TO DO
    private void Score(Vector3 pos)
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
        _IScore = IScore(pos);
        StartCoroutine(_IScore);
    }

    private IEnumerator IScore(Vector3 pos)
    {
        _isScore = true;
        _mapper.GetAreaStrength(pos, _bombCreator.BombRadius, out float hum, out float red);
        float humScore = humanCoef * hum;
        float redScore = redCoef * red;
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
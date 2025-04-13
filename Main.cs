using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Main : MonoBehaviour
{
    [SerializeField] private MenuUI _menuUI;
    [SerializeField] private Mapper _mapper;
    [SerializeField] private float _timerStartValue = 60.1f;
    [SerializeField] private float _timerStartSpeed = 4f;
    [SerializeField] private float _horaCenterWait = 5f;

    [SerializeField] private float humanCoef = 1f;
    [SerializeField] private float redCoef = 1f;


    public enum GameState
    {
        Idle,
        Game,
        Defeat,
    }

    private GameState _gameState = GameState.Idle;

    public UnityAction<GameState> OnGameStateChanged;

    public GameState GetGameState { get { return _gameState; } }

    public void Explode(float human, float red)
    {
        _menuUI.ChangeTimer(red * redCoef - human * humanCoef, human, red);
    }

    public void ReturnToMainMenu()
    {
        if (_gameState == GameState.Game)
        {
            PauseGame();
        }
    }

    private void Awake()
    {
        _menuUI._OnTimer = () => StartCoroutine(IHoraCenter());
        _menuUI._OnHoraCenter += () => _menuUI.StartTimer(_timerStartValue, _timerStartSpeed);
        _menuUI._OnHoraUp += OnGameStart;

        _menuUI._OnGameMenu = ContinueGame;
    }

    private IEnumerator IHoraCenter()
    {
        yield return new WaitForSeconds(_horaCenterWait);
        _menuUI.HoraUp();
    }

    private void OnGameStart()
    {
        _menuUI._OnTimer = Defeat;
        _menuUI._OnHoraUp -= OnGameStart;
        _menuUI._OnHoraUp += ContinueGame;
        ContinueGame();
    }

    private void ContinueGame()
    {
        SetGameState(GameState.Game);
        _menuUI.StartTimer(0f, 1f);
    }

    private void PauseGame()
    {
        SetGameState(GameState.Idle);
        _menuUI.StopTimers();
        _menuUI.ToMainMenu();
    }

    private void Defeat()
    {
        Debug.Log("Lost!");
        SetGameState(GameState.Defeat);

        _menuUI.StopTimers();
        _menuUI.ShowDefeat();

        _menuUI._OnMainMenu += ResetMapper;
        _menuUI._OnTimer -= Defeat;
        _menuUI._OnHoraUp -= ContinueGame;
        _menuUI._OnHoraUp += OnGameStart;
        _menuUI._OnTimer = () => StartCoroutine(IHoraCenter());
    }

    private void ResetMapper()
    {
        _mapper.Reset();
        _menuUI._OnMainMenu -= ResetMapper;
    }

    private void SetGameState(GameState gameState)
    {
        _gameState = gameState;
        OnGameStateChanged?.Invoke(gameState);
    } 
}

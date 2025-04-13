using UnityEngine;
using UnityEngine.InputSystem;

public class Input : MonoBehaviour
{
    [SerializeField] private CameraMover _cameraMover;
    [SerializeField] private BombCreator _bombCreator;
    [SerializeField] private Main _main;
    [SerializeField] private float ScrollWheelSpeed = 1.0f;

    private InputAction InputScroll;
    private InputAction InputLook;
    private InputAction InputAttack;
    private InputAction InputMove;

    private InputAction InputInteract;

    private bool isGame;

    private void Start()
    {
        InputMove = InputSystem.actions.FindAction("Move");
        InputLook = InputSystem.actions.FindAction("Look");
        InputAttack = InputSystem.actions.FindAction("Attack");
        InputInteract = InputSystem.actions.FindAction("Interact");
        InputScroll = InputSystem.actions.FindAction("ScrollWheel");

        _main.OnGameStateChanged += (Main.GameState gameState) =>
        {
            isGame = gameState == Main.GameState.Game;
            if (isGame)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
        };
    }

    void Update()
    {
        if (isGame)
        {
            Vector2 lookDirection = InputLook.ReadValue<Vector2>();
            Vector2 moveDirection = InputMove.ReadValue<Vector2>();
            bool f1 = InputAttack.WasPressedThisFrame();
            bool isInteract = InputInteract.WasPressedThisFrame();
            float scrollWheel = InputScroll.ReadValue<Vector2>().y;

            //Debug.Log("LOOK - " + lookDirection.ToString());
            //Debug.Log("MOVE - " + moveDirection.ToString());
            //Debug.Log("SCROLL - " + scrollWheel.ToString());
            //Debug.Log("F1 - " + f1.ToString());

            _bombCreator.MoveBomb(lookDirection);
            _cameraMover.CameraLook(moveDirection);
            if (f1)
            {
                if (_bombCreator.IsBombCreated)
                {
                    _bombCreator.Explode();
                }
                else
                {
                    _bombCreator.CreateBomb();
                }
            }

            if (isInteract)
            {
                _main.ReturnToMainMenu();
            }

            if (scrollWheel != 0)
            {
                _bombCreator.MoveBombScroll(scrollWheel);
            }

        }
    }
}

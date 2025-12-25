using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Input : MonoBehaviour
{
    public UnityAction<Vector2> OnMove;
    public UnityAction<Vector2> OnLook;
    public UnityAction OnAttack1;
    public UnityAction OnAttack2;
    public UnityAction OnInteract;
    public UnityAction<float> OnScroll;

    private InputAction InputMove;
    private InputAction InputLook;
    private InputAction InputAttack1;
    private InputAction InputAttack2;
    private InputAction InputInteract;
    private InputAction InputScroll;

    public bool IsWorking;

    private void Start()
    {
        InputMove = InputSystem.actions.FindAction("Move");
        InputLook = InputSystem.actions.FindAction("Look");
        InputAttack1 = InputSystem.actions.FindAction("Attack1");
        InputAttack2 = InputSystem.actions.FindAction("Attack2");
        InputInteract = InputSystem.actions.FindAction("Interact");
        InputScroll = InputSystem.actions.FindAction("ScrollWheel");
    }

    private void Update()
    {
        if (IsWorking)
        {
            Vector2 moveDirection = InputMove.ReadValue<Vector2>();
            Vector2 lookDirection = InputLook.ReadValue<Vector2>();
            bool f1 = InputAttack1.WasPressedThisFrame();
            bool f2 = InputAttack2.WasPressedThisFrame();
            bool isInteract = InputInteract.WasPressedThisFrame();
            float scrollWheel = InputScroll.ReadValue<Vector2>().y;

            OnMove?.Invoke(moveDirection);
            OnLook?.Invoke(lookDirection);
            if (f1)
            {
                OnAttack1?.Invoke();
            }
            if (f2)
            {
                OnAttack2?.Invoke();
            }
            if (isInteract)
            {
                OnInteract?.Invoke();
            }
            if (scrollWheel != 0)
            {
                OnScroll?.Invoke(scrollWheel);
            }
        }
    }
}

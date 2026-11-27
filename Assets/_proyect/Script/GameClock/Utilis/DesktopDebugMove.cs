using UnityEngine;
using UnityEngine.InputSystem;

namespace Tape0317.Utils
{
    [RequireComponent(typeof(CharacterController))]
    public class DesktopDebugMove : MonoBehaviour
    {
        [Header("Teclas (cámbialas si el simulador ya las usa)")]
        [SerializeField] Key forwardKey = Key.I;
        [SerializeField] Key backKey = Key.K;
        [SerializeField] Key turnLeftKey = Key.J;
        [SerializeField] Key turnRightKey = Key.L;
        [SerializeField] Key runKey = Key.RightShift;

        [Header("Movimiento")]
        [SerializeField, Min(0.1f)] float walkSpeed = 2.5f;
        [SerializeField, Min(0.1f)] float runSpeed = 4f;
        [Tooltip("Grados por segundo al girar.")]
        [SerializeField, Min(1f)] float turnSpeed = 90f;
        [SerializeField] float gravity = -9.81f;

        CharacterController controller;
        Transform head;
        float verticalVelocity;

        void Awake()
        {
#if !UNITY_EDITOR
            // En el Quest no existe: se usa el joystick de verdad.
            enabled = false;
            return;
#endif
            controller = GetComponent<CharacterController>();
        }

        void Start()
        {
            if (Camera.main != null)
                head = Camera.main.transform;
        }

        bool Pressed(Keyboard kb, Key key) => key != Key.None && kb[key].isPressed;

        void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || head == null || !controller.enabled)
                return;

            float turn = 0f;
            if (Pressed(kb, turnLeftKey)) turn -= 1f;
            if (Pressed(kb, turnRightKey)) turn += 1f;
            if (turn != 0f)
                transform.RotateAround(head.position, Vector3.up, turn * turnSpeed * Time.deltaTime);

            float forwardInput = 0f;
            if (Pressed(kb, forwardKey)) forwardInput += 1f;
            if (Pressed(kb, backKey)) forwardInput -= 1f;

            Vector3 forward = head.forward;
            forward.y = 0f;
            forward.Normalize();

            float speed = Pressed(kb, runKey) ? runSpeed : walkSpeed;
            Vector3 move = forward * (forwardInput * speed);

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -1f;
            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);
        }
    }
}

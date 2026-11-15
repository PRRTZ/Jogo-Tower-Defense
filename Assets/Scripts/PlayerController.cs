
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float moveSpeed = 5f;
    public float runSpeed = 9f;
    public float jumpHeight = 1.5f;
    public float gravity = -15f;

    [Header("Suavização da Animação")]
    public float animDampTime = 0.1f;

    private CharacterController controller;
    private Animator animator;

    private Vector3 velocity;
    private bool jumpRequested;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("Animator não encontrado nos objetos filhos do Player!");
        }
    }

    void Start()
    {
        if (animator != null)
        {
            animator.SetBool("IsGrounded", true);
            Debug.Log("Speed enviado: " + animator.GetFloat("Speed"));
            animator.SetFloat("Speed", 0f);
        }
    }

    void Update()
    {
        // 1. Leitura do comando de pulo
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }

        // 2. Verifica se está no chão
        if (controller.isGrounded)
        {
            if (velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            // Executa o pulo
            if (jumpRequested)
            {
                velocity.y = Mathf.Sqrt(
                    jumpHeight * -2f * gravity
                );

                jumpRequested = false;
            }
        }

        // 3. Leitura dos controles de movimentação
        float moveX = 0f;
        float moveZ = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed)
                moveX += 1f;

            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
                moveX -= 1f;

            if (Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed)
                moveZ += 1f;

            if (Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed)
                moveZ -= 1f;
        }

        Vector3 moveDirection = new Vector3(
            moveX,
            0f,
            moveZ
        ).normalized;

        Debug.Log("Teclado: " + Keyboard.current);
        Debug.Log("Movimento: " + moveDirection);
        Debug.Log("Animator: " + animator);

        // 4. Rotação do personagem
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        // 5. Verifica se está correndo
        bool isRunning = Keyboard.current != null &&
                         Keyboard.current.leftShiftKey.isPressed;

        float currentSpeed = isRunning ? runSpeed : moveSpeed;

        // Se não estiver movimentando, a velocidade será zero
        if (moveDirection.sqrMagnitude < 0.01f)
        {
            currentSpeed = 0f;
        }

        // 6. Aplicação da gravidade
        velocity.y += gravity * Time.deltaTime;

        // 7. Movimentação física
        Vector3 finalMove =
            (moveDirection * currentSpeed) +
            new Vector3(0f, velocity.y, 0f);

        controller.Move(finalMove * Time.deltaTime);

        // 8. Atualização do Animator
        if (animator != null)
        {
            // Atualiza se o personagem está no chão
            animator.SetBool(
                "IsGrounded",
                controller.isGrounded
            );

            // Atualiza a velocidade da Blend Tree
            float targetAnimSpeed = currentSpeed;

            animator.SetFloat(
                "Speed",
                targetAnimSpeed,
                animDampTime,
                Time.deltaTime
            );
        }

        // 9. Limpa solicitações de pulo enquanto estiver no ar
        if (!controller.isGrounded)
        {
            jumpRequested = false;
        }
    }
}

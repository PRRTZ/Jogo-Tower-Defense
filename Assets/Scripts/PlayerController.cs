using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Configurações (Ajustáveis no Inspector)")]
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -15f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool jumpRequested;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1. Guarda a intenção de pular imediatamente quando a tecla é premida
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }

        // 2. Se estiver no chão, ajusta a gravidade base e executa o pulo se foi pedido
        if (controller.isGrounded)
        {
            if (velocity.y < 0)
            {
                velocity.y = -2f; // Mantém o player colado ao chão
            }

            if (jumpRequested)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpRequested = false; // Reseta o pedido
            }
        }

        // 3. Lê o movimento horizontal (WASD / Setas)
        float moveX = 0f;
        float moveZ = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ -= 1f;
        }

        Vector3 moveDirection = new Vector3(moveX, 0f, moveZ).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        // 4. Aplica a gravidade continuamente
        velocity.y += gravity * Time.deltaTime;

        // 5. Junta o movimento horizontal e vertical num ÚNICO comando Move (essencial para o isGrounded)
        Vector3 finalMove = (moveDirection * moveSpeed) + new Vector3(0, velocity.y, 0);
        controller.Move(finalMove * Time.deltaTime);

        // Se o player saltou e saiu do chão, limpa o pedido pendente
        if (!controller.isGrounded)
        {
            jumpRequested = false;
        }
    }
}
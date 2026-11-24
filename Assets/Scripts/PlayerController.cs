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

    [Header("Chão / Degraus")]
    public float groundSnapDistance = 0.6f;   // até quantos metros ele desce "colado" no chão
    public float groundedGrace = 0.15f;       // tempo que ainda conta como no chão para o Animator

    [Header("Câmera")]
    public bool relativeToCamera = true;      // true = W/A/S/D relativos à câmera (qualquer rotação)

    [Header("Mira com o mouse")]
    [Tooltip("Desligado (padrão): olha para onde anda e só vira para o mouse ao atacar. " +
             "Ligado: sempre olha para o mouse.")]
    public bool alwaysFaceMouse = false;
    [Tooltip("Graus por segundo ao girar para o mouse quando 'Always Face Mouse' está ligado.")]
    public float faceMouseTurnSpeed = 1080f;
    [Tooltip("Segundos que o personagem continua virado para o mouse depois de atacar (cobre o golpe).")]
    public float attackFacingTime = 0.45f;

    private float aimLockUntil;               // até quando mantém a direção do ataque

    private CharacterController controller;
    private Animator animator;
    private Camera cam;

    private Vector3 velocity;
    private bool jumpRequested;
    private float lastGroundedTime;           // última vez que estava no chão

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        cam = Camera.main;

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
            animator.SetFloat("Speed", 0f);
        }
    }

    void Update()
    {
        if (PauseSettingsMenu.IsOpen) return;

        // 1. Leitura do comando de pulo
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }

        // 2. Verifica se está no chão (resultado do Move do frame anterior)
        bool groundedBefore = controller.isGrounded;

        if (groundedBefore)
        {
            lastGroundedTime = Time.time;

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

        Vector3 moveDirection;

        if (relativeToCamera)
        {
            if (cam == null) cam = Camera.main;

            // Direção da câmera projetada no plano horizontal
            Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;
            Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveDirection = (camForward * moveZ + camRight * moveX).normalized;
        }
        else
        {
            moveDirection = new Vector3(moveX, 0f, moveZ).normalized;
        }

        // 4. Rotação do personagem: durante o golpe mantém a direção do mouse;
        //    fora dele olha para onde anda (ou sempre para o mouse, se ligado)
        if (Time.time < aimLockUntil)
        {
            // segura a direção do ataque
        }
        else if (alwaysFaceMouse && TryGetMouseDirection(out Vector3 aimDir))
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(aimDir), faceMouseTurnSpeed * Time.deltaTime);
        }
        else if (moveDirection.sqrMagnitude > 0.01f)
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

        // 7.1 Saiu de um degrau sem pular? Gruda no chão logo abaixo
        if (!controller.isGrounded && groundedBefore && velocity.y <= 0f)
        {
            SnapToGround();
        }

        // 8. Atualização do Animator
        if (animator != null)
        {
            // Tolerância curta: um soluço de 1-2 frames sem chão não dispara a animação de queda
            bool animGrounded = controller.isGrounded ||
                                (velocity.y <= 0f && Time.time - lastGroundedTime < groundedGrace);

            animator.SetBool("IsGrounded", animGrounded);

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

    /// <summary>Direção horizontal do personagem até o ponto do chão embaixo do mouse.</summary>
    public bool TryGetMouseDirection(out Vector3 dir)
    {
        dir = Vector3.zero;
        var mouse = Mouse.current;
        if (mouse == null) return false;
        if (cam == null) cam = Camera.main;
        if (cam == null) return false;

        // plano horizontal na altura do personagem: não depende de colliders no chão
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        Plane ground = new Plane(Vector3.up, transform.position);
        if (!ground.Raycast(ray, out float dist)) return false;

        dir = ray.GetPoint(dist) - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.04f) return false;   // mouse em cima do personagem: mantém a direção
        dir.Normalize();
        return true;
    }

    /// <summary>Vira o personagem na hora para o mouse e segura essa direção durante o golpe (usado pelo ataque).</summary>
    public void SnapToMouse()
    {
        if (TryGetMouseDirection(out Vector3 dir))
            transform.rotation = Quaternion.LookRotation(dir);
        aimLockUntil = Time.time + attackFacingTime;
    }

    // Desce o personagem até o chão se ele estiver a até groundSnapDistance de distância
    void SnapToGround()
    {
        Vector3 origin = controller.bounds.center;
        float halfHeight = controller.bounds.extents.y;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                            halfHeight + groundSnapDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            float drop = hit.distance - halfHeight;
            if (drop > 0.001f)
            {
                controller.Move(Vector3.down * drop);
                velocity.y = -2f;          // zera a velocidade de queda acumulada
            }
        }
    }
}
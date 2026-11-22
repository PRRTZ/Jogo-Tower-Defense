using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerSounds : MonoBehaviour
{
    public float walkInterval = 0.45f;     // tempo entre passos andando
    public float runInterval = 0.30f;      // tempo entre passos correndo
    public float runSpeedThreshold = 7f;   // velocidade a partir da qual conta como corrida

    CharacterController cc;
    float stepTimer;

    void Awake() { cc = GetComponent<CharacterController>(); }

    void Update()
    {
        Vector3 v = cc.velocity;
        float horizontal = new Vector3(v.x, 0f, v.z).magnitude;

        if (cc.isGrounded && horizontal > 0.5f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                SoundManager.Play(Sfx.Footstep);
                stepTimer = horizontal >= runSpeedThreshold ? runInterval : walkInterval;
            }
        }
        else stepTimer = 0f;   // o próximo passo toca logo ao começar a andar
    }
}
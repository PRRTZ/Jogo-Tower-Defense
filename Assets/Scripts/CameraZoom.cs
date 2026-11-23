using UnityEngine;
using Unity.Cinemachine;

public class CameraZoom : MonoBehaviour
{
    [Header("Referências")]
    public CinemachineCamera cinemachineCamera;

    [Header("Configurações de Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 3f;   // Mais perto (zoom in)
    public float maxZoom = 30f;  // Mais longe (visão panorâmica)
    
    [Tooltip("Distância padrão em que a câmara vai iniciar ao dar Play")]
    public float defaultZoom = 12f; 

    private CinemachineFollow followComponent;

    void Start()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }

        if (cinemachineCamera != null)
        {
            followComponent = cinemachineCamera.GetComponent<CinemachineFollow>();
            
            // Força a câmara a iniciar na distância padrão ao dar Play
            if (followComponent != null)
            {
                Vector3 currentOffset = followComponent.FollowOffset;
                if (currentOffset.magnitude > 0)
                {
                    followComponent.FollowOffset = currentOffset.normalized * defaultZoom;
                }
            }
        }
    }

    void Update()
    {
        if (PauseSettingsMenu.IsOpen) return;
        if (followComponent == null) return;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0f)
        {
            Vector3 currentOffset = followComponent.FollowOffset;
            float currentDistance = currentOffset.magnitude;

            currentDistance -= scrollInput * zoomSpeed; 
            currentDistance = Mathf.Clamp(currentDistance, minZoom, maxZoom);

            if (currentOffset.magnitude > 0)
            {
                followComponent.FollowOffset = currentOffset.normalized * currentDistance;
            }
        }
    }
}
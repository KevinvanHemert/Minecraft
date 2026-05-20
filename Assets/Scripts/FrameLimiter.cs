using UnityEngine;

public class FrameLimiter : MonoBehaviour
{
    [Header("Frame Rate")]
    [SerializeField] private int targetFrameRate = 60;

    [Header("VSync")]
    [SerializeField] private bool useVSync = false;

    private void Start()
    {
        QualitySettings.vSyncCount = useVSync ? 1 : 0;
        Application.targetFrameRate = targetFrameRate;
    }
}
using UnityEngine;

public class GoalController : MonoBehaviour
{
    [Header("Эффекты гола")]
    public ParticleSystem goalEffect;
    public AudioClip goalSoundClip; 

    [Header("Настройки звука")]
    public float goalSoundVolume = 1.0f;
    private AudioSource audioSource;

    [Header("Goal Cooldown")]
    public float goalCooldown = 1f;
    private bool canScore = true;
    private float lastGoalTime;

    void Start()
    {
        if (goalEffect == null)
            goalEffect = GetComponentInChildren<ParticleSystem>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; 
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Football")) return;
        if (!canScore) return;
        if (!IsBallEnteringGoal(other)) return;

        Debug.Log("Гол!!! Мяч в воротах!");

        PlayGoalEffects();

        if (GameManager.instance != null)
        {
            GameManager.instance.GoalScored(other.gameObject);
        }

        StartCooldown();
    }

    bool IsBallEnteringGoal(Collider ball)
    {
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null) return true;

        Vector3 ballVelocity = rb.linearVelocity;
        if (ballVelocity.magnitude < 0.1f) return false;
        Vector3 toGoalCenter = transform.position - ball.transform.position;
        float angle = Vector3.Angle(ballVelocity.normalized, toGoalCenter.normalized);

        return angle < 90f;
    }

    void PlayGoalEffects()
    {
        // Эффекты частиц
        if (goalEffect != null)
        {
            goalEffect.Play();
        }

        // Звук гола
        if (goalSoundClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(goalSoundClip, goalSoundVolume);
        }
        else if (goalSoundClip == null)
        {
            Debug.LogWarning("Не назначен звук гола! Перетащите аудиоклип в Goal Sound Clip");
        }

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    void StartCooldown()
    {
        canScore = false;
        lastGoalTime = Time.time;
        Invoke("ResetCooldown", goalCooldown);
    }

    void ResetCooldown()
    {
        canScore = true;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            BoxCollider collider = GetComponent<BoxCollider>();
            if (collider != null)
            {
                Gizmos.DrawCube(transform.position + collider.center, collider.size);
            }
        }
    }
}
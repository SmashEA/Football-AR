using UnityEngine;

public class FootballController : MonoBehaviour
{
    private Rigidbody rb;
    private bool isKicked = false;
    private float kickTime;

    [Header("Настройки")]
    public float destroyAfterKick = 5f;
    public float minGoalSpeed = 1f; 

    [Header("Звук удара")]
    public AudioClip kickSound;
    public float kickVolume = 1.0f;
    private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }


        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
        audioSource.volume = kickVolume;
        audioSource.maxDistance = 50f;
    }

    public void Kick(Vector3 force, Vector3 position)
    {
        if (isKicked) return;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        StartCoroutine(ApplyKick(force));

        isKicked = true;
        kickTime = Time.time;

        PlayKickSound();

        Invoke("AutoDestroy", destroyAfterKick);
    }

    System.Collections.IEnumerator ApplyKick(Vector3 force)
    {
        yield return null;
        if (rb != null)
        {
            rb.AddForce(force, ForceMode.Impulse);
        }
    }
    void PlayKickSound()
    {
        if (kickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(kickSound, kickVolume);
            Debug.Log("Воспроизведен звук удара по мячу");
        }
        else
        {
            Debug.LogWarning("Не назначен звук удара или отсутствует AudioSource!");
        }
    }

    void AutoDestroy()
    {
        if (GameManager.instance != null && !GameManager.instance.IsGoalScored(gameObject))
        {
            Debug.Log("Мяч не забит - уничтожаю");
            Destroy(gameObject);
        }
    }

    public void DestroyAfterDelay(float delay)
    {
        CancelInvoke("AutoDestroy");
        Destroy(gameObject, delay);
    }

    public bool IsKicked()
    {
        return isKicked;
    }

    public float GetSpeed()
    {
        return rb != null ? rb.linearVelocity.magnitude : 0f;
    }
}
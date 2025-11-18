using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundAreaGate : MonoBehaviour
{
    [Header("Distance Settings")]
    [SerializeField] private float innerRadius = 3f;
    [SerializeField] private float outerRadius = 8f;

    [Header("Sound Settings")]
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private float maxVolume = 1f;
    [SerializeField] private bool loopSound = true;

    [Header("References (optional)")]
    [SerializeField] private Transform player;

    private AudioSource source;
    private float targetVolume = 0f;
    private bool isInside = false;
    private bool isZoneEnabled = false; 
    private bool hasInitialized = false;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = loopSound;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.Stop(); 
    }

    private void Start()
    {
        
        isZoneEnabled = false;
        source.Stop();
        source.volume = 0f;
        hasInitialized = true;
    }

    private void Update()
    {
        if (!hasInitialized || !isZoneEnabled)
        {
            if (source.isPlaying)
            {
                source.Stop();
                source.volume = 0f;
            }
            return;
        }

        if (player == null)
        {
            if (PlayerController.PlayerTransform != null)
                player = PlayerController.PlayerTransform;
            else
                return;
        }

        if (source.clip == null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        if (dist <= innerRadius)
        {
            targetVolume = maxVolume;
            isInside = true;
        }
        else if (dist <= outerRadius)
        {
            float t = Mathf.InverseLerp(outerRadius, innerRadius, dist);
            targetVolume = Mathf.Lerp(0f, maxVolume, 1f - t);
            isInside = true;
        }
        else
        {
            targetVolume = 0f;
            isInside = false;
        }

        float newVolume = Mathf.MoveTowards(source.volume, targetVolume, fadeSpeed * Time.deltaTime);

        if (isInside && isZoneEnabled)
        {
            if (!source.isPlaying)
                source.Play();
            source.volume = newVolume;
        }
        else
        {
            source.volume = newVolume;
            if (source.isPlaying && source.volume <= 0.01f)
            {
                source.Stop();
                source.volume = 0f;
            }
        }
    }

    public void EnableZone(bool enable)
    {
        isZoneEnabled = enable;

        if (source == null)
            source = GetComponent<AudioSource>();

        if (source == null)
            return;

        if (!enable)
        {
            if (source.isPlaying)
            {
                source.Stop();
                source.volume = 0f;
            }
        }
        else
        {
            source.volume = 0f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, innerRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, outerRadius);
    }
#endif
}

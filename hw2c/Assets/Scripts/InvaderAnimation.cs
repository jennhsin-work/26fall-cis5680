using UnityEngine;

public class InvaderAnimation : MonoBehaviour
{
    public float pulseRate = 1.0f;
    public Vector3 alternateScale = new Vector3(1.15f, 0.85f, 1.0f);

    private Vector3 initialScale;
    private float timer = 0f;
    private bool frameToggle = false;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= (1f / Mathf.Max(pulseRate, 0.1f)))
        {
            timer = 0f;
            frameToggle = !frameToggle;
            transform.localScale = frameToggle ? Vector3.Scale(initialScale, alternateScale) : initialScale;
        }
    }

    public void SetSpeed(float speed)
    {
        pulseRate = speed;
    }
}

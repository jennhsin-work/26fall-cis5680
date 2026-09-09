using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonoBehaviour
{
    public int pointValue;
    public GameObject deathExplosion;
    public AudioClip deathKnell;

    // Start is called before the first frame update
    void Start()
    {
        pointValue = 10;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void Die()
    {
        if (deathKnell != null)
        {
            AudioSource.PlayClipAtPoint(deathKnell, gameObject.transform.position);
        }
        if (deathExplosion != null)
        {
            Instantiate(deathExplosion, gameObject.transform.position, Quaternion.AngleAxis(-90, Vector3.right));
        }
        GameObject obj = GameObject.Find("GlobalObject");
        if (obj != null)
        {
            Global g = obj.GetComponent<Global>();
            if (g != null)
            {
                g.score += pointValue;
            }
        }
        Destroy(gameObject);
    }
}


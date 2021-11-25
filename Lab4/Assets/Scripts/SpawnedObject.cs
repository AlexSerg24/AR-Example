using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnedObject : MonoBehaviour
{
    private string displayName;
    private string description;
    private bool shouldDestroy = false;
    private GameObject body;
    private GameObject Particles;

    private int counter = 0;

    public GameObject ParticlesDestroy
    {
        get
        {
            return Particles;
        }
        set
        {
            Particles = value;
        }
    }

    public GameObject Object
    {
        get
        {
            return body;
        }
        set
        {
            body = value;
        }
    }

    public string Name
    {
        get
        {
            return displayName;
        }
        set
        {
            displayName = value;
        }
    }
    public string Description
    {
        get
        {
            return description;
        }
        set
        {
            description = value;
        }
    }

    public bool ShouldDestroy
    {
        get
        {
            return shouldDestroy;
        }
        set
        {
            shouldDestroy = value;
        }
    }

    void Update()
    {
        if (shouldDestroy)
        {
            if (counter <= 36)
            {
                body.transform.localScale = body.transform.localScale - new Vector3(0.005f, 0.005f, 0.005f);
                counter++;
            }
            else
            {
                StartCoroutine(AddParticles(gameObject.transform.position));
                Destroy(gameObject);
            }
        }
    }
    private IEnumerator AddParticles(Vector3 pos)
    {
        GameObject particles = Instantiate(Particles, pos, Particles.transform.rotation);
        yield return new WaitForSeconds(3f);
        Destroy(particles);
    }
}
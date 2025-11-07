using UnityEngine;
using System.Collections;

public class Sleeping : MonoBehaviour
{

    public GameObject sleepingFX;
    public ParticleSystem sleepingZzz;
    public AudioSource snoring;

    void Start()
    {

        sleepingFX.SetActive(false);

    }


    void Update()
    {

        if (Input.GetButtonDown("Fire1")) //check to see if the left mouse was pressed - trigger firework
        {

            sleepingFX.SetActive(true);
            sleepingZzz.Play();
            snoring.Play();

        }

        if (Input.GetButtonUp("Fire1")) //check to see if the left mouse was pressed - trigger firework
        {

            sleepingZzz.Stop();
            snoring.Stop();

        }

    }

}
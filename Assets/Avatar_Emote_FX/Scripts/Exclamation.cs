using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exclamation : MonoBehaviour {

    public GameObject exclamationFX;

	// Use this for initialization
	void Start () {

        exclamationFX.SetActive(false);

    }
	
	// Update is called once per frame
	void Update () {

        if (Input.GetButtonDown("Fire1")) //check to see if the left mouse was pushed.
        {

            StartCoroutine("ExclamationOn");

        }

    }

    IEnumerator ExclamationOn()
    {


        exclamationFX.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        exclamationFX.SetActive(false);

    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitScript : MonoBehaviour
{
    public void OnCollisionExit(Collision collision)
    {
        {
            Application.Quit();
        }
    }
}

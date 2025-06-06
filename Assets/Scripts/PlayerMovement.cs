using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour 
{
    private void Start()
    {
        
    }
    private void Update()
    {
        transform.position += Time.deltaTime * new Vector3(0, 0, -2);   
    }
}
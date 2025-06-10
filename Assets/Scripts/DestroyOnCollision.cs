using UnityEngine;

public class Collision_Manager_bullet : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Walls")
        {
            Debug.Log("Bullet Destroyed on Collision");
            Destroy(gameObject);
        }
    }
}
using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    public float lifetime = 3f; // seconds before disappearing

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
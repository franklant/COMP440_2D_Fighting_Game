using UnityEngine;
public class SelfDestruct : MonoBehaviour {
    public float lifeTime = 0.5f;
    void Start() { Destroy(gameObject, lifeTime); }
}
using UnityEngine;

public class Delivery : MonoBehaviour
{
    [SerializeField] ParticleSystem particle;
    bool hasPackage = false;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Package") && !hasPackage)
        {
            Debug.Log("PickUp Package");
            hasPackage = true;
            //Destroy(collision.gameObject,0.5f);
            particle.Play();
        }
        if(collision.CompareTag("Customer") && hasPackage)
        {
            Debug.Log("Delivered");
            Destroy(collision.gameObject);
            hasPackage = false;
            particle.Stop();
        }
    }
}


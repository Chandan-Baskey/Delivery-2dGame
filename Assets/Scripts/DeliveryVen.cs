using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;  
using TMPro;

public class DeliveryVen : MonoBehaviour
{
    [SerializeField]float currentSpeed = 5f;
    [SerializeField]float rotSpeed = 200f;
    [SerializeField]float boost = 10f;
    [SerializeField]float regularSpeed = 5f;
    [SerializeField] TMP_Text boostText;

    void Start()
    {
        
    }

    void Update()
    {
        float mov = 0;
        float rot = 0;
        if (Keyboard.current.wKey.isPressed)
        {
           // Debug.Log("W key is pressed forword");
            mov = 1;
        }

        else if(Keyboard.current.sKey.isPressed)
            {
                //Debug.Log("S key is pressed backword");
                mov = -1;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            //Debug.Log("A key is pressed left");
            rot = 1;
        }

        else if (Keyboard.current.dKey.isPressed)
        {
           // Debug.Log("D key is pressed right");
            rot = -1;
        }

        float movAmount = mov * currentSpeed * Time.deltaTime;
        float rotAmount = rot * rotSpeed * Time.deltaTime;

        transform.Rotate(0, 0,rotAmount );
        transform.Translate(0, movAmount, 0);

        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Boost"))
        {
            Debug.Log("Boost Activated");
            currentSpeed = boost;
            boostText.gameObject.SetActive(true);
            Destroy(collision.gameObject, 0.5f);
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        currentSpeed = regularSpeed;
        boostText.gameObject.SetActive(false);
    }
}


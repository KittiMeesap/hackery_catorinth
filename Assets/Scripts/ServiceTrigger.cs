using UnityEngine;

public class ServiceTrigger : MonoBehaviour
{
    public static ServiceTrigger Instance;

    public CustomerController currentCustomer;
    public bool playerInside = false;
    public PlayerInventory playerInventory;

    private void Awake()
    {
        Instance = this;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Customer"))
        {
            currentCustomer = other.GetComponent<CustomerController>();
        }

        if (other.CompareTag("Player"))
        {
            playerInside = true;
            playerInventory = other.GetComponent<PlayerInventory>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Customer") && currentCustomer != null &&
            other.gameObject == currentCustomer.gameObject)
        {
            currentCustomer = null;
        }

        if (other.CompareTag("Player"))
        {
            playerInside = false;
            playerInventory = null;
        }
    }

    public void ClearCurrentCustomer()
    {
        currentCustomer = null;
        playerInside = false;
    }

}

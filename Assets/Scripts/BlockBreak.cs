using UnityEngine;

public class Block : MonoBehaviour
{
    void OnMouseDown()
    {
        // Sol tık = kır
        if (Input.GetMouseButtonDown(0))
        {
            Destroy(gameObject);
        }
    }
}

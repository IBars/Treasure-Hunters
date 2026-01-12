using UnityEngine;

public class BlockPlacer : MonoBehaviour
{
    public GameObject blockPrefab;
    public float range = 5f;

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // sağ tık
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, range))
            {
                Vector3 placePos = hit.point + hit.normal * 0.5f;

                placePos = new Vector3(
                    Mathf.Round(placePos.x),
                    Mathf.Round(placePos.y),
                    Mathf.Round(placePos.z)
                );

                Instantiate(blockPrefab, placePos, Quaternion.identity);
            }
        }
    }
}

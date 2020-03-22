using UnityEngine;

public class LightningSpawn : MonoBehaviour
{
    private GameObject lightningArc;
    private float timeUntilNextSpawn;
    // Start is called before the first frame update
    void Start()
    {
        lightningArc = Resources.Load<GameObject>("Lightning VFX/LightningArc");
        timeUntilNextSpawn = 0;
    }

    private void Update()
    {
        Debug.Log("update");
        if (timeUntilNextSpawn < Time.time)
        {
            Debug.Log("instantiating lightning");
            GameObject arc = Instantiate(lightningArc, transform.position, Quaternion.identity);
            arc.transform.parent = gameObject.transform;
            timeUntilNextSpawn = Time.time + Random.Range(0, 5);
        }
    }
}

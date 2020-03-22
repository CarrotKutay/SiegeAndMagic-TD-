using UnityEngine;

public class LightningSpawn : MonoBehaviour
{
    private GameObject lightningArc;
    private float timeUntilNextSpawn;
    private Unity.Mathematics.Random randomGenerator;
    // Start is called before the first frame update
    void Start()
    {
        lightningArc = Resources.Load<GameObject>("Lightning VFX/LightningArc");
        timeUntilNextSpawn = 0;
        randomGenerator = new Unity.Mathematics.Random((uint)0xfffff);
    }

    private void Update()
    {
        if (timeUntilNextSpawn < Time.time)
        {
            GameObject arc = Instantiate(lightningArc, transform.position, Quaternion.identity);
            arc.GetComponent<LightningArc>().RandomGenerator = new Unity.Mathematics.Random(randomGenerator.NextUInt());
            arc.transform.parent = gameObject.transform;
            timeUntilNextSpawn = Time.time + Random.Range(0, 5);
        }
    }
}

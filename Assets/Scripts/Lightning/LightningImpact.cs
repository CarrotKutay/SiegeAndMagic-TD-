using UnityEngine;

public class LightningImpact : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Material electricalImpact = Resources.Load<Material>("PBR_Electricity/electricity_material");
        transform.rotation = Quaternion.AngleAxis(90, Vector3.right);
        transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        Destroy(GetComponent<Collider>());

        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
        renderer.material = electricalImpact;
        renderer.shadowCastingMode = 0;
        Destroy(gameObject, 1f);
    }
}

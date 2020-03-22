using UnityEngine;

public class LightningArc : MonoBehaviour
{
    ///<summary>
    /// <see cref="origin"/> is the starting point of the lightning arc to be created. By default set to (0, 0, 0)
    ///</summary>
    private Vector3 origin;
    ///<summary>
    /// <see cref="destination"/> is the end-point of the arc to be created
    ///</summary>
    private Vector3 destination;
    ///<summary>
    /// The direction from the current position of the <see cref="LightningArc"/> to the <see cref="destination"/>
    ///</summary>
    private Vector3 dirToDest
    {
        get
        {
            if (nextSegmentCounter <= 1) return (destination - origin).normalized;
            return (destination - lineRenderer.GetPosition(nextSegmentCounter)).normalized;
        }
    }

    public float MaxRadius { get { return subemitter ? maxRadius / 2 : maxRadius; } set => maxRadius = value; }

    public Unity.Mathematics.Random RandomGenerator { get => randomGenerator; set => randomGenerator = value; }

    [SerializeField]
    private Material lightningArcMaterial;
    private LineRenderer lineRenderer;
    ///<summary>
    /// Maximum distance from origin for any arc that a lightning arc is able to reach
    ///</summary>
    [SerializeField]
    private float maxRadius = 10f;
    private float startWidth = .2f;
    private Unity.Mathematics.Random randomGenerator;
    private Plane constraintPlane;
    private int nextSegmentCounter;
    private float timeUntilDeath;
    private GameObject lightningArcTip;
    private LightningTip tip;
    private bool subemitter = false;
    private bool hasForked = false;



    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lightningArcTip = new GameObject();
        lightningArcTip.transform.parent = transform;
        tip = lightningArcTip.AddComponent<LightningTip>();
        tip.OnCollisionDetected += createCollision;

        nextSegmentCounter = 0;
        setupLightningArc();
        timeUntilDeath = UnityEngine.Random.Range(.3f, .8f);
        Destroy(gameObject, timeUntilDeath);
    }

    private void setupLightningArc()
    {
        origin = transform.position;
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, origin); // setting start position
        setRandomDestination();
        lineRenderer.material = lightningArcMaterial;
        lineRenderer.endWidth = 0;
        if (subemitter) lineRenderer.startWidth = startWidth / 2;
        else lineRenderer.startWidth = startWidth;
        lineRenderer.numCapVertices = 3;
    }

    private void addArcSegment()
    {
        nextSegmentCounter++;
        lineRenderer.positionCount++;

        Vector3 nextPosition = addNoiseToArc() + lineRenderer.GetPosition(nextSegmentCounter - 1);
        tip.TipPosition = nextPosition;
        lineRenderer.SetPosition(nextSegmentCounter, nextPosition);
    }

    private Vector3 addNoiseToArc()
    {
        float x_multiplier = UnityEngine.Random.Range(0, 1f);
        float y_multiplier = UnityEngine.Random.Range(0, 1f);
        float z_multiplier = UnityEngine.Random.Range(0, 1f);

        Vector3 directionWithNoise = new Vector3(
            dirToDest.x * x_multiplier * 1.5f,
            dirToDest.y * y_multiplier * 1.5f,
            dirToDest.z * z_multiplier * 1.5f
        );

        return directionWithNoise;
    }

    ///<summary>
    ///Create a random destiantion with a maximum distance away from <see cref="origin"/>: sqrt(3*(pow(<see cref="MaxRadius"/>, 2)))
    ///</summary>
    private void setRandomDestination(bool constraint = false)
    {

        destination = new Vector3(UnityEngine.Random.Range(-MaxRadius, MaxRadius),
                                 UnityEngine.Random.Range(-MaxRadius, MaxRadius),
                                 UnityEngine.Random.Range(-MaxRadius, MaxRadius));
        if (constraint)
        {
            if (!constraintPlane.GetSide(destination)) setRandomDestination(true);
        }
    }

    private void FixedUpdate()
    {
        addArcSegment();
        if (!hasForked) forkLightning();
    }

    private void forkLightning()
    {
        if (randomGenerator.NextInt(0, 10) < 6)
        {
            hasForked = true;
            createSubEmitters(tip.TipPosition);
        }
    }

    private void createCollision(object sender, LightningTip.OnCollisionDetectedEventArgs e)
    {
        tip.OnCollisionDetected -= createCollision;
        if (sender == lightningArcTip as object)
        {
            Vector3 arcPosition = lineRenderer.GetPosition(nextSegmentCounter),
                   arcDirection = dirToDest;

            createImpactPoint(e.collision.point);
            if (!subemitter) createSubEmitters(e.collision.point, e.collision.normal);

            gameObject.SetActive(false);
            Destroy(lightningArcTip);
            Destroy(gameObject, .3f);
        }

    }

    private void createImpactPoint(Vector3 position)
    {
        GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Quad);
        impact.transform.position = position;
        impact.AddComponent<LightningImpact>();
    }

    private void createSubEmitters(Vector3 origin, Vector3 originNormal = new Vector3())
    {
        // randomly deciding on number of subemitters
        int numberOfSubEmitters = UnityEngine.Random.Range(0, 5);
        GameObject arc = Resources.Load<GameObject>("Lightning VFX/LightningArc");

        for (int i = 0; i < numberOfSubEmitters; i++)
        {
            GameObject subArc = Instantiate(arc, origin + originNormal.normalized * 2, Quaternion.identity);
            subArc.transform.parent = transform;
            LightningArc lightning = subArc.GetComponent<LightningArc>();
            lightning.subemitter = true;
            lightning.hasForked = hasForked;
            if (originNormal != Vector3.zero)
            {
                lightning.constraintPlane = new Plane();
                lightning.constraintPlane.SetNormalAndPosition(originNormal, origin);
                lightning.startWidth = .1f;
            }
        }
    }

    private void OnDestroy()
    {
        if (lightningArcTip != null) Destroy(lightningArcTip);
    }


}

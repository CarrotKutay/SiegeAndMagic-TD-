using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using System.Collections.Generic;

public class LightningArc : MonoBehaviour
{
    [SerializeField]
    private float lightningDelay = 1f;
    ///<summary>
    /// <see cref="Origin"/> is the starting point of the lightning arc to be created. By default set to (0, 0, 0)
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
            if (nextSegmentCounter <= 1) return (destination - Origin).normalized;
            return (destination - lineRenderer.GetPosition(nextSegmentCounter)).normalized;
        }
    }

    public float MaxRadius { get { return subemitter ? maxRadius / 2 : maxRadius; } set => maxRadius = value; }
    public Unity.Mathematics.Random RandomGenerator { get => randomGenerator; set => randomGenerator = value; }
    public Vector3 Origin { get => origin; set => origin = value; }
    [SerializeField]
    private Material lightningArcMaterial;
    private LineRenderer lineRenderer;
    private List<LightningData> lightningDataList;
    ///<summary>
    /// Maximum distance from origin for any arc that a lightning arc is able to reach
    ///</summary>
    [SerializeField]
    private float maxRadius = 10f;
    [SerializeField]
    private float startWidth = .2f;
    private Unity.Mathematics.Random randomGenerator;
    private Plane constraintPlane;
    private int nextSegmentCounter;
    [SerializeField]
    private float timeUntilDeath;
    private GameObject lightningArcTip;
    private LightningTip tip;
    [SerializeField]
    private bool subemitter = false;
    [SerializeField]
    private bool hasForked = false;
    [SerializeField]
    private bool hasCollided = false;


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
    }

    // setup line renderer
    private void setupLightningArc()
    {
        Origin = transform.position;
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, Origin); // setting start position
        setRandomDestination();
        lineRenderer.material = lightningArcMaterial;
        lineRenderer.endWidth = 0;
        if (subemitter) lineRenderer.startWidth = startWidth / 2;
        else lineRenderer.startWidth = startWidth;
        lineRenderer.numCapVertices = 3;
    }

    // adding new positions to line renderer to extend the lightning
    private void addArcSegment()
    {
        if (!hasCollided)
        {
            nextSegmentCounter++;
            lineRenderer.positionCount++;

            Vector3 nextPosition = addNoiseToArc() + lineRenderer.GetPosition(nextSegmentCounter - 1);
            tip.TipPosition = nextPosition;
            lineRenderer.SetPosition(nextSegmentCounter, nextPosition);
        }
    }

    // adding noise to the lightning (instead of straight line)
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
    ///Create a random destiantion with a maximum distance away from <see cref="Origin"/>: sqrt(3*(pow(<see cref="MaxRadius"/>, 2)))
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

    // using fixed update as we are handling collisions 
    // and are therefore including physics behaviour to be
    // considered
    private void FixedUpdate()
    {
        updateTime();
        addArcSegment();

        if (!hasForked) forkLightning(); // lightning won't fork if it has forked before
        if (timeUntilDeath <= 0) Destroy(gameObject); // lightning will be destroyed automatically if timeOfDeath is reached
        if (lightningDataList != null) moveLightningPositions(); // spread lightning 
    }

    // update time left lightning to be alive
    private void updateTime()
    {
        timeUntilDeath -= Time.deltaTime;
    }

    // forking lightning at lightning tip (40% chance at each frame of it happening)
    private void forkLightning()
    {
        if (randomGenerator.NextInt(0, 10) > 5)
        {
            hasForked = true;
            createSubEmitters(tip.TipPosition);
        }
    }

    // handling of collision of lightning with objects
    // ! right now it does not recognize all collisions as the 
    // ! lightning is moving to fast for the phsics calculations
    private void createCollision(object sender, LightningTip.OnCollisionDetectedEventArgs e)
    {
        tip.OnCollisionDetected -= createCollision;
        if (sender == lightningArcTip as object)
        {
            hasCollided = true;
            Vector3 arcPosition = lineRenderer.GetPosition(nextSegmentCounter),
                   arcDirection = dirToDest;

            createImpactPoint(e.collision.point);
            if (!subemitter) createSubEmitters(new Vector3(tip.TipPosition.x, tip.TipPosition.y, tip.TipPosition.z), e.collision.normal);
            else if (lineRenderer.positionCount <= 2) Destroy(gameObject);
        }

        spreadLightningPositions();
    }

    // method to imitate 'lightning gravity' of it spreading and
    // lingering after hitting and colliding with another object
    // uses a a parallel job (multithreading)
    private void spreadLightningPositions()
    {
        if (hasCollided && lineRenderer.positionCount > 2)
        {
            timeUntilDeath += lightningDelay;
            NativeList<LightningData> lightningData = new NativeList<LightningData>(lineRenderer.positionCount, Allocator.TempJob);

            LightningPhysics physicsJob = new LightningPhysics()
            {
                dataList = lightningData.AsParallelWriter(),
                numberOfPositions = lineRenderer.positionCount,
                peak = (int)(lineRenderer.positionCount / 2),
                gravityFloatingMultiplier = 3f,
                gravityFloatingDirection = Vector3.up
            };

            JobHandle physicsJobHandle = physicsJob.Schedule(lineRenderer.positionCount, 32);
            physicsJobHandle.Complete();
            lightningDataList = new List<LightningData>(lightningData.ToArray());

            lightningData.Dispose();
        }
    }

    // move all lightning positions according to values calculated in the
    // multithreaded job to perform 'lightning physics'
    private void moveLightningPositions()
    {
        foreach (var item in lightningDataList)
        {
            Vector3 position = lineRenderer.GetPosition(item.index);
            Vector3 direction = item.direction;
            if (timeUntilDeath > 0)
            {
                position += (direction - position) * Time.deltaTime;
                lineRenderer.SetPosition(item.index, position);
                timeUntilDeath -= Time.deltaTime;
            }
        }
    }

    // create an impact point at the surface hit upon collision
    private void createImpactPoint(Vector3 position)
    {
        GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Quad);
        impact.transform.position = position;
        impact.AddComponent<LightningImpact>();
    }

    // creating subemitters either due to collision or simply 
    // as a fork of this lightning
    // ? right now time of death of subemitters is not tied to there parent
    // ? lightning which emitted them -> should be changed?
    private void createSubEmitters(Vector3 origin, Vector3 originNormal = new Vector3())
    {
        // randomly deciding on number of subemitters
        int numberOfSubEmitters = UnityEngine.Random.Range(0, 5);
        GameObject arc = Resources.Load<GameObject>("Lightning VFX/LightningArc");

        for (int i = 0; i < numberOfSubEmitters; i++)
        {
            GameObject subArc = Instantiate(arc, origin, Quaternion.identity);
            //subArc.transform.parent = transform;
            LightningArc lightning = subArc.GetComponent<LightningArc>();
            lightning.subemitter = true;
            lightning.hasForked = hasForked;
            lightning.randomGenerator = new Unity.Mathematics.Random(randomGenerator.NextUInt());
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
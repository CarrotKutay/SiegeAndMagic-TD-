using UnityEngine;
using System;
using Unity.Mathematics;

public class LightningTip : MonoBehaviour
{
    private float3 tipPosition;
    public EventHandler<OnCollisionDetectedEventArgs> OnCollisionDetected;
    private Rigidbody rb;
    public class OnCollisionDetectedEventArgs : EventArgs
    {
        public ContactPoint collision;
    }
    private SphereCollider tipCollider;

    public float3 TipPosition
    {
        get => tipPosition; set
        {
            tipPosition = value;
            rb.MovePosition(tipPosition);
        }
    }

    private void Awake()
    {
        tipCollider = gameObject.AddComponent<SphereCollider>();
        rb = gameObject.AddComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        gameObject.name = "Lightning Tip";
        tipCollider.radius = .2f;
        rb.useGravity = false;
    }

    private void OnCollisionEnter(Collision other)
    {
        OnCollisionDetected?.Invoke(gameObject, new OnCollisionDetectedEventArgs
        {
            collision = other.GetContact(0)
        });
    }
}

using UnityEngine;
using System;

public class LightningTip : MonoBehaviour
{
    public EventHandler<OnCollisionDetectedEventArgs> OnCollisionDetected;
    public Rigidbody rb;

    public class OnCollisionDetectedEventArgs : EventArgs
    {
        public ContactPoint collision;
    }
    private SphereCollider tipCollider;
    // Start is called before the first frame update
    void Start()
    {
        gameObject.name = "Lightning Tip";
        tipCollider = gameObject.AddComponent<SphereCollider>();
        tipCollider.radius = .2f;
        rb = gameObject.AddComponent<Rigidbody>();
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToStartPos : HaptoFeedback 
{

    public float posK = 350f;
    public float rotK = 10f;
    public float maxForce = 200f;
    public float maxTorque = 20f;
    public float randomForce = 30f;
    public float randomTorque = 0.1f;
    public float randomSpeed = 10f;
    public float ratioTouched = 0.5f;
    public float coeffForceX = 1f;
    public float coeffForceY = 1f;
    public float coeffForceZ = 1f;

    Vector3 posInitial;
    Vector3 posTarget;
    Vector3 top = new Vector3(0, 1, 0);
    Vector3 topTarget;
    Vector3 axisA;
    Vector3 axisB;
    Quaternion rotInitial;
    new Rigidbody rigidbody;
    bool first = true;

    Vector3 centerSrd;
    Vector3 axisXsrd = new Vector3(1,0,0);
    Vector3 axisYsrd = new Vector3(0, 1, 1);
    Vector3 axisZsrd = new Vector3(0, -1, 1);
    Vector3 vBase;
    Vector3 vCenter;

    void Start()
    {
        posInitial = transform.position;
        rotInitial = transform.rotation;
        rigidbody = GetComponent<Rigidbody>();
      }

    public override void Feedback(ref List<HaptoObject> haptObjects, float[] point, int numPoint)
    {
        float ratio = 1f;
        if (haptObjects[0].numClusterIn > 0)
            ratio = ratioTouched;
        
        posTarget = posInitial;
        var pos = transform.position;
        Vector3 force = posK * (posTarget - pos) * ratio;
        if (force.magnitude > maxForce)
            force = force.normalized * maxForce;
        force.x *= coeffForceX;
        force.y *= coeffForceY;
        force.z *= coeffForceZ;
        rigidbody.AddForce(force);



        //var y1 = transform.rotation * top;
        //Vector3 torque = rotK * Vector3.Cross(y1, topTarget);
        //if (torque.magnitude > maxTorque)
        //    torque = torque.normalized * maxTorque;
        ////Debug.Log("T: " + torque.magnitude);

        //rigidbody.AddTorque(torque);

        Quaternion deltaRotation = rotInitial * Quaternion.Inverse(transform.rotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        //if (Mathf.Abs(angle) < 0.1f) continue;

        Vector3 torque = axis.normalized * angle * rotK * ratio;
        rigidbody.AddTorque(torque, ForceMode.Force);


        Vector3 f = new Vector3();
        f.x = Mathf.PerlinNoise(Time.time * randomSpeed, 0) * 2 - 1;
        f.y = Mathf.PerlinNoise(0, Time.time * randomSpeed) * 2 - 1;
        f.z = Mathf.PerlinNoise(Time.time * randomSpeed, Time.time * randomSpeed) * 2 - 1;
        f = f * randomForce;
        rigidbody.AddForce(f);

        Vector3 t = new Vector3();
        t.x = Mathf.PerlinNoise(Time.time * randomSpeed, 0) * 2 - 1;
        t.y = Mathf.PerlinNoise(0, Time.time * randomSpeed) * 2 - 1;
        t.z = Mathf.PerlinNoise(Time.time * randomSpeed, Time.time * randomSpeed) * 2 - 1;
        t = t * randomTorque;
        rigidbody.AddTorque(t);
    }

}

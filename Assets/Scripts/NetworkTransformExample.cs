using NetworkEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reference only
/// </summary>
public class NetworkTransformExample : NetworkComponent
{
    //Position, velocity, rotation, angular velocity
    public Vector3 LastPosition;
    public Vector3 LastRotation;
    public Vector3 LastVelocity;
    public Vector3 LastAngular;

    public Vector3 OffsetVelocity;
    public float Threshold = .1f;

    public float EThreshold = 2.5f;
    public Rigidbody MyRig;

    public override void HandleMessage(string command, List<string> args)
    {
        if (command == "POS" && IsClient)
        {
            //Parse out our position
            //Update LastPosition
            //Find thrshold.
            //Asssuming we are below the emergency 
            float d = (MyRig.position - LastPosition).magnitude;
            if (d > EThreshold)
            {
                MyRig.position = LastPosition;
                OffsetVelocity = Vector3.zero;
            }
            else
            {
                //We want to add to the velocity to make up for the latency by next update.   
                //Speed = distance/time
                //distance = difference in positions, time = delay between sending updates.  (In our case .1)
                OffsetVelocity = (LastPosition - MyRig.velocity) / .1f;
                //You may need to play with this a little.
                //Also you may want to set the OffsetVelocity to vector3.Zero IF the distance is really small.
            }

        }
        if (command == "VEL" && IsClient)
        {
            Vector3 tempVel = Vector3.zero;
            //Vector3 tempVel = <Parse values;>
            //Usually you do not want to have any delay on the velocity.  
            //However, do not forget the offset velocity.
            MyRig.velocity = tempVel + OffsetVelocity;
        }

        if (command == "ROT" && IsClient)
        {
            //Parse out rotaiton and set it.
        }
        if (command == "ANG" && IsClient)
        {
            //Parse out angular velocity and set it.
        }
    }

    protected override IEnumerator NetworkUpdate()
    {
        while (true)
        {
            if (IsServer)
            {

            }
            yield return new WaitForSeconds(.1f);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        MyRig = GetComponent<Rigidbody>();
    }
}
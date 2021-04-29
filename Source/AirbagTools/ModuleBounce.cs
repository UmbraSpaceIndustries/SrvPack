using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Public Domain by SQUAD

public class USI_ModuleBounce : PartModule
{
    [KSPField(isPersistant = false)]
    public float bounciness = 0.5f;

    public override void OnStart(PartModule.StartState state)
    {
        ModuleBounceCollider bounce = gameObject.GetComponent<ModuleBounceCollider>();

        if (bounce == null)
        {
            bounce = gameObject.AddComponent<ModuleBounceCollider>();
            bounce.bounciness = bounciness;
            bounce.part = part;
        }
    }
}

public class ModuleBounceCollider : MonoBehaviour
{
    public float bounciness = 0.5f;
    public Part part;

    Vector3 lastVel = Vector3.zero;
    void FixedUpdate()
    {
        if (GetComponent<Rigidbody>())
            lastVel = GetComponent<Rigidbody>().velocity;
    }

    private void OnCollisionEnter(Collision col)
    {
        try
        {
            Vector3 normal = Vector3.zero;
            foreach (ContactPoint c in col.contacts)
                normal += c.normal;
            normal.Normalize();
            Vector3 inVelocity = lastVel;
            Vector3 outVelocity = bounciness * (-2f * (Vector3.Dot(inVelocity, normal) * normal) + inVelocity);
            GetComponent<Rigidbody>().velocity = outVelocity;
        }
        catch (Exception ex)
        {
            print("[AB] Error in OnCollissionEnter - " + ex.Message);            
        }
    }
}
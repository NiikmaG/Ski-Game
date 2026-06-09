using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowmanThrow : MonoBehaviour
{
    public GameObject snowBall;
    public float throwDistance;
    public int throwSpeed;
    private bool justThrown = false;

    void Update()
    {
        GameObject target = GameObject.FindWithTag("Player");
        if (target == null || snowBall == null) return;
       
        float distanceToTarget = Vector3.Distance(target.transform.position, transform.position);

        if (distanceToTarget < throwDistance && justThrown == false)
        {
            justThrown = true;
            GameObject tempSnowBall = Instantiate(snowBall, transform.position, transform.rotation);
            if (tempSnowBall.GetComponent<ObstacleDetector>() == null)
                tempSnowBall.AddComponent<ObstacleDetector>();

            Rigidbody tempRb = tempSnowBall.GetComponent<Rigidbody>();
            if (tempRb == null)
            {
                justThrown = false;
                return;
            }

            Vector3 targetDirection = Vector3.Normalize(target.transform.position - transform.position);
            
            targetDirection += new Vector3(0, 0.33f, 0);
            tempRb.AddForce(targetDirection * throwSpeed);
            Invoke(nameof(ThrowOver), 0.45f);
        }
    }

    void ThrowOver()
    {
        justThrown = false;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RacketAgent : Agent
{   
    private Rigidbody racketRb;
    public Transform ball;
    private Rigidbody ballRb;
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 100.0f;
    private float successTimer = 0f;
    private float successDuration = 1f;

    // Start is called before the first frame update
    void Start()
    {
        racketRb = GetComponent<Rigidbody>();
        ballRb = ball.GetComponent<Rigidbody>();
    }
    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        this.transform.localPosition = new Vector3(-0.2f, 0.3f, 0.03f);
        this.transform.localEulerAngles = Vector3.zero;
        racketRb.velocity = Vector3.zero;
        racketRb.angularVelocity = Vector3.zero;

        ballRb.velocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        // ball.localPosition = new Vector3(0, Random.Range(0.8f, 1.2f), 0);
        ball.localPosition = new Vector3(0, 1f, 0);

        successTimer = 0f;
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);
        sensor.AddObservation(ball.localPosition);
        sensor.AddObservation(ballRb.velocity);

        sensor.AddObservation(this.transform.localPosition);
        sensor.AddObservation(this.transform.localEulerAngles.z);
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        base.OnActionReceived(actions);
        Vector3 movement = Vector3.zero;
        movement.x = actions.ContinuousActions[0] * moveSpeed * Time.deltaTime;
        movement.y = actions.ContinuousActions[1] * moveSpeed * Time.deltaTime;
        transform.position += movement;

        float rotate = actions.ContinuousActions[2] * rotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.forward, rotate);

        float distance = Vector3.Distance(ball.localPosition, transform.localPosition);
        AddReward(-distance * 0.01f);
        AddReward(-Mathf.Abs(ballRb.velocity.y) * 0.01f);

        if(distance < 0.1f && ballRb.velocity.magnitude < 0.1f){
            AddReward(0.05f);
            successTimer += Time.fixedDeltaTime;
            if(successTimer >= successDuration){
                Debug.Log("Succeed");
                AddReward(2.0f);
                EndEpisode();
            }
        }
        else{
            successTimer = 0f;
            AddReward(-0.001f);
        }

        if(ball.localPosition.y < -0.1f){
            AddReward(-1.0f);
            EndEpisode();
        }
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        base.Heuristic(actionsOut);
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}

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
    private float timer = 0f;
    private float duration = 5f;
    private float successTimer = 0f;
    private float successDuration = 1f;
    private int totalEpisodes = 0;
    private int successEpisodes = 0;

    // Start is called before the first frame update
    void Start()
    {
        racketRb = GetComponent<Rigidbody>();
        ballRb = ball.GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        
        // this.transform.localPosition = new Vector3(Random.Range(-0.3f, 0.3f), 0.3f, 0.03f);
        this.transform.localPosition = new Vector3(0, 0.3f, 0.03f);
        this.transform.localEulerAngles = new Vector3(0, 0, Random.Range(-30f, 30f));

        racketRb.velocity = Vector3.zero;
        racketRb.angularVelocity = Vector3.zero;

        ballRb.velocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        ball.localPosition = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.8f, 1.2f), 0);

        timer = 0f;
        successTimer = 0f;

        totalEpisodes++;
        // Debug.Log("Total Episodes: " + totalEpisodes + ", Success Episodes: " + successEpisodes);
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
        // Action
        Vector3 movement = Vector3.zero;
        movement.x = actions.ContinuousActions[0] * moveSpeed * Time.deltaTime;
        movement.y = actions.ContinuousActions[1] * moveSpeed * Time.deltaTime;
        transform.position += movement;

        // Boundaries
        if (transform.localPosition.y > 2.0f) {
            transform.localPosition = new Vector3(transform.localPosition.x, 2.0f, transform.localPosition.z);
        }
        if (transform.localPosition.x > 2.0f) {
            transform.localPosition = new Vector3(2.0f, transform.localPosition.y, transform.localPosition.z);
        }
        if (transform.localPosition.x < -2.0f) {
            transform.localPosition = new Vector3(-2.0f, transform.localPosition.y, transform.localPosition.z);
        }

        float rotate = actions.ContinuousActions[2] * rotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.forward, rotate);

        /* Rewards */

        // distance penalty
        float distance = Vector3.Distance(ball.localPosition, transform.localPosition);
        AddReward(-distance * 0.01f);
        
        // velocity penalty
        if (ballRb.velocity.magnitude > 0.1f) {
            AddReward(-Mathf.Abs(ballRb.velocity.magnitude) * 0.01f);
        }
        
        // only if the ball is still moving, the racket should align with the ball
        if (distance > 0.1f) {
            // encourage racket to move align with ball
            Vector3 racketNormal = transform.up;
            // Debug.Log("racketNormal: " + racketNormal);

            Vector3 dirc2ball = ball.localPosition - transform.localPosition;
            float alignment = Mathf.Abs(Vector3.Dot(racketNormal, dirc2ball.normalized));
            // Debug.Log("alignment: " + alignment);

            
            if (alignment < 0.2f) { // reward for alignment
                AddReward((0.2f - alignment) * 0.001f);
            }
            if (alignment > 0.8f) { // penalty for misalignment
                AddReward((0.8f - alignment) * 0.001f);
            }
        }

        // Holding the Ball
        if(distance < 0.1f && ballRb.velocity.magnitude < 0.1f){

            float distRacket2Floor = Mathf.Abs(transform.localPosition.y);
            // float vanillaReward = 0.003f + 0.005f * (Mathf.Max(0f, (0.5f - distRacket2Floor)) / 0.5f);
            float vanillaReward = 0.05f;
            if (distRacket2Floor > 0.1f && distRacket2Floor < 0.5f)
                vanillaReward += 0.03f;
            AddReward(vanillaReward);
            // AddReward(0.05f);

            successTimer += Time.fixedDeltaTime;
            // if(successTimer >= successDuration || Mathf.Abs(ballRb.velocity.y) < 0.075f){
            if(successTimer >= successDuration){
                Debug.Log("Succeed");
                AddReward(5.0f);
                successEpisodes++;
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

        timer += Time.fixedDeltaTime;
        if (timer >= duration) {
            if (successTimer > 0f) {
                Debug.Log("Succeed");
                AddReward(5.0f);
                successEpisodes++;
            }
            else {
                AddReward(-1.0f);
            }
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        base.Heuristic(actionsOut);
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[2] = Input.GetAxis("Vertical");
    }
}

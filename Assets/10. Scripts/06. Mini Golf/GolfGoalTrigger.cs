using UnityEngine;
namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(BoxCollider))]
    public class GolfGoalTrigger : MonoBehaviour
    {
        private void Awake() { GetComponent<BoxCollider>().isTrigger = true; }
        private void OnTriggerEnter(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null) return;
            GolfBall ball = body.GetComponent<GolfBall>();
            if (ball != null && GolfRoundFlow.Instance != null)
                GolfRoundFlow.Instance.CompleteHole(ball);
        }
    }
}

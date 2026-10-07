using UnityEngine;
namespace Study3D.MiniGolf
{
    // 맵 아래 넓은 Box Trigger. 높이 기준 자동 복귀에 추가해서 사용 가능.
    [RequireComponent(typeof(BoxCollider))]
    public class GolfFallTrigger : MonoBehaviour
    {
        private void Awake() { GetComponent<BoxCollider>().isTrigger = true; }
        private void OnTriggerEnter(Collider other) { ReturnBall(other); }
        private void OnTriggerStay(Collider other) { ReturnBall(other); }
        private void ReturnBall(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null || GolfRoundFlow.Instance == null) return;
            GolfBall ball = body.GetComponent<GolfBall>();
            if (ball != null) GolfRoundFlow.Instance.RespawnBall(ball);
        }
    }
}

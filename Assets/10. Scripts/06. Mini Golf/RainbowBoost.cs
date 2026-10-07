using System.Collections.Generic;
using UnityEngine;
namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(BoxCollider))]
    public class RainbowBoost : MonoBehaviour
    {
        [Tooltip("월드 좌표 기준 가속 방향. X 또는 Z를 길 방향에 맞추세요")]
        public Vector3 direction = Vector3.forward;
        [Min(0f)] public float acceleration = 8f;
        [Min(0f)] public float maxForwardSpeed = 18f;
        private readonly Dictionary<Rigidbody, float> lastStep = new Dictionary<Rigidbody, float>();
        private void Awake() { GetComponent<BoxCollider>().isTrigger = true; }
        private void OnDisable() { lastStep.Clear(); }
        private void OnTriggerEnter(Collider other) { Boost(other); }
        private void OnTriggerStay(Collider other) { Boost(other); }
        private void Boost(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.isKinematic) return;
            GolfBall ball = body.GetComponent<GolfBall>();
            if (ball == null) return;
            if (lastStep.TryGetValue(body, out float stamp) && stamp == Time.fixedTime) return;
            lastStep[body] = Time.fixedTime;
            Vector3 forward = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.5f) return;
            Vector3 horizontal = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            // 멈춘 공은 자동 출발시키지 않습니다.
            if (horizontal.sqrMagnitude < 0.0001f) return;
            //ball.NotifyExternalMotion();
            float speed = Vector3.Dot(horizontal, forward);
            float delta = Mathf.Min(Mathf.Max(0f, acceleration) * Time.fixedDeltaTime,
                Mathf.Max(0f, maxForwardSpeed - speed));
            if (delta > 0f) body.AddForce(forward * delta, ForceMode.VelocityChange);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 d = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            Gizmos.DrawRay(transform.position, d * 2f);
        }
    }
}

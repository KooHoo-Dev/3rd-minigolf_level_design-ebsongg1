using System.Collections.Generic;
using UnityEngine;
namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(BoxCollider))]
    public class JumpPad : MonoBehaviour
    {
        [Min(0f)] public float jumpSpeed = 4f;
        [Tooltip("월드 좌표 기준 착지 구역 방향")]
        public Vector3 forwardDirection = Vector3.forward;
        [Min(0f)] public float minForwardSpeed = 2f;
        private BoxCollider zone;
        private readonly HashSet<Rigidbody> locked = new HashSet<Rigidbody>();
        private readonly List<Rigidbody> release = new List<Rigidbody>();
        private void Awake() { zone = GetComponent<BoxCollider>(); zone.isTrigger = true; }
        private void OnDisable() { locked.Clear(); }
        private void FixedUpdate()
        {
            release.Clear();
            foreach (Rigidbody body in locked)
                if (body == null || (OutsideFootprint(body) && Landed(body))) release.Add(body);
            foreach (Rigidbody body in release) locked.Remove(body);
        }
        private bool OutsideFootprint(Rigidbody body)
        {
            Vector3 p = zone.transform.InverseTransformPoint(body.worldCenterOfMass) - zone.center;
            Vector3 half = zone.size * 0.5f;
            // 위로 벗어나는 것은 재사용 조건이 아닙니다. X/Z 밖으로 나가야 함.
            return Mathf.Abs(p.x) > half.x + 0.05f || Mathf.Abs(p.z) > half.z + 0.05f;
        }
        private bool Landed(Rigidbody body)
        {
            if (Mathf.Abs(body.linearVelocity.y) > 0.3f) return false;
            SphereCollider sphere = body.GetComponent<SphereCollider>();
            if (sphere == null) return false;
            Vector3 origin = sphere.bounds.center;
            float distance = sphere.bounds.extents.y + 0.08f;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (hit.collider.attachedRigidbody != body && hit.normal.y > 0.5f) return true;
            return false;
        }
        private void OnTriggerEnter(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.isKinematic || locked.Contains(body)) return;
            GolfBall ball = body.GetComponent<GolfBall>();
            if (ball == null) return;
            Vector3 forward = Vector3.ProjectOnPlane(forwardDirection, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.5f)
            { Debug.LogWarning("JumpPad: Forward Direction에 X 또는 Z 방향을 지정하세요.", this); return; }
            locked.Add(body);
            Vector3 velocity = body.linearVelocity;
            float forwardSpeed = Vector3.Dot(velocity, forward);
            velocity += forward * Mathf.Max(0f, minForwardSpeed - forwardSpeed);
            // 느리게 재진입하거나 낙하 중이어도 일정한 위쪽 출발 속도 확보.
            velocity.y = Mathf.Max(velocity.y, jumpSpeed);
            body.linearVelocity = velocity;
            ball.NotifyExternalMotion();
            body.WakeUp();
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position,
                Vector3.ProjectOnPlane(forwardDirection, Vector3.up).normalized * 2f);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
namespace Study3D.MiniGolf
{
    // 이 스크립트와 실제 충돌 Collider를 같은 오브젝트에 둡니다.
    public class JellyBumper : MonoBehaviour
    {
        public Collider targetCollider;
        [Min(1f)] public float speedMultiplier = 1.2f;
        [Min(0f)] public float minBounceSpeed = 4f;
        [Min(0.1f)] public float maxBounceSpeed = 18f;
        private PhysicsMaterial material;
        private PhysicsMaterial original;
        private readonly Dictionary<Rigidbody, float> nextBounce = new Dictionary<Rigidbody, float>();
        private void Awake()
        {
            if (targetCollider == null)
                foreach (Collider c in GetComponents<Collider>())
                    if (c.enabled && !c.isTrigger) { targetCollider = c; break; }
            if (targetCollider == null || !targetCollider.enabled || targetCollider.gameObject != gameObject)
            {
                Debug.LogError("JellyBumper: 같은 오브젝트의 활성 Collider를 지정하세요.", this);
                enabled = false; return;
            }
            original = targetCollider.sharedMaterial;
            targetCollider.isTrigger = false;
            material = new PhysicsMaterial("Jelly_CodeBounce");
            material.staticFriction = 0f;
            material.dynamicFriction = 0f;
            material.bounciness = 0f;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            targetCollider.sharedMaterial = material;
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (!enabled || collision.contactCount == 0) return;
            Rigidbody body = collision.rigidbody;
            if (body == null || body.isKinematic) return;
            GolfBall ball = body.GetComponent<GolfBall>();
            if (ball == null) return;
            if (nextBounce.TryGetValue(body, out float next) && Time.time < next) return;
            nextBounce[body] = Time.time + 0.15f;
            ContactPoint contact = collision.GetContact(0);
            Vector3 normal = Vector3.ProjectOnPlane(contact.normal, Vector3.up);
            Vector3 outward = Vector3.ProjectOnPlane(body.worldCenterOfMass - contact.point, Vector3.up);
            if (normal.sqrMagnitude < 0.0001f) normal = outward;
            if (normal.sqrMagnitude < 0.0001f) return;
            normal.Normalize();
            if (Vector3.Dot(normal, outward) < 0f) normal = -normal;
            Vector3 incoming = Vector3.ProjectOnPlane(collision.relativeVelocity, Vector3.up);
            // 상대 속도의 부호와 관계없이 젤리를 향하는 입사 방향으로 정리.
            if (Vector3.Dot(incoming, normal) > 0f) incoming = -incoming;
            Vector3 reflected = incoming.sqrMagnitude > 0.0001f
                ? Vector3.Reflect(incoming, normal).normalized : normal;
            float speed = Mathf.Clamp(incoming.magnitude * speedMultiplier,
                Mathf.Min(minBounceSpeed, maxBounceSpeed), maxBounceSpeed);
            // 위쪽 속도는 유지하고 수평 반사 속도만 설정.
            body.linearVelocity = reflected * speed + Vector3.up * body.linearVelocity.y;
            ball.NotifyExternalMotion();
            body.WakeUp();
        }
        private void OnDisable() { nextBounce.Clear(); }
        private void OnDestroy()
        {
            if (targetCollider != null && material != null) targetCollider.sharedMaterial = original;
            if (material != null) Destroy(material);
        }
    }
}

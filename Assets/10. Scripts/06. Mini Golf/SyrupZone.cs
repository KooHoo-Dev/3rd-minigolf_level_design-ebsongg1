using System.Collections.Generic;
using UnityEngine;

namespace Study3D.MiniGolf
{
    // 바닥과 별개인 Box Collider 트리거에 붙이세요.
    [RequireComponent(typeof(BoxCollider))]
    public class SyrupZone : MonoBehaviour
    {
        [Tooltip("클수록 빠르게 감속. 먼저 2로 테스트")]
        [Min(0f)] public float slowdown = 2f;
        private readonly Dictionary<Rigidbody, float> lastStep = new Dictionary<Rigidbody, float>();

        private void Awake() { GetComponent<BoxCollider>().isTrigger = true; }
        private void OnDisable() { lastStep.Clear(); }

        private void OnTriggerStay(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.isKinematic || body.GetComponent<GolfBall>() == null) return;
            // 공에 콜라이더가 여러 개 있어도 한 물리 스텝에 한 번만 감속.
            if (lastStep.TryGetValue(body, out float stamp) && stamp == Time.fixedTime) return;
            lastStep[body] = Time.fixedTime;
            float factor = Mathf.Exp(-Mathf.Max(0f, slowdown) * Time.fixedDeltaTime);
            Vector3 v = body.linearVelocity;
            body.linearVelocity = new Vector3(v.x * factor, v.y, v.z * factor);
            body.angularVelocity *= factor;
        }
    }
}

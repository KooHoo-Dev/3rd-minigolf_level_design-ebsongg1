using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
namespace Study3D.MiniGolf
{
    public class GolfRoundUI : MonoBehaviour
    {
        [Tooltip("한국어로 바꾸려면 한글 지원 Font 지정. 기본 UI 문구는 영어")]
        public Font font;
        private GameObject canvasObject;
        private Text hud, title, description;
        private RectTransform panel;
        private CanvasGroup group;
        private Button restart;
        private Text buttonLabel;
        private Coroutine animationRoutine;
        private void Awake()
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvasObject = new GameObject("GolfRoundCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            hud = MakeText("HUD", canvasObject.transform, new Vector2(0, 305), new Vector2(700, 65), 28);
            hud.color = Color.white;
            panel = MakeRect("ClearResultPanel", canvasObject.transform, Vector2.zero, new Vector2(650, 480));
            panel.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.08f, 0.16f, 0.96f);
            group = panel.gameObject.AddComponent<CanvasGroup>();
            title = MakeText("Title", panel, new Vector2(0, 155), new Vector2(600, 70), 42);
            description = MakeText("Details", panel, new Vector2(0, 10), new Vector2(590, 210), 27);
            RectTransform buttonRect = MakeRect("Restart", panel, new Vector2(0, -170), new Vector2(310, 65));
            buttonRect.gameObject.AddComponent<Image>().color = new Color(0.8f, 0.35f, 0.55f);
            restart = buttonRect.gameObject.AddComponent<Button>();
            restart.targetGraphic = buttonRect.GetComponent<Image>();
            buttonLabel = MakeText("Label", buttonRect, Vector2.zero, new Vector2(300, 60), 25);
            buttonLabel.text = "BACK TO HOLE 1";
            HideOverlay();
            if (EventSystem.current == null)
            {
                GameObject es = new GameObject("GolfEventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
        }
        private RectTransform MakeRect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false); rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }
        private Text MakeText(string name, Transform parent, Vector2 pos, Vector2 size, int fontSize)
        {
            Text t = MakeRect(name, parent, pos, size).gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = fontSize; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false; return t;
        }
        public void SetHUD(int hole, int count, int shots) { hud.text = $"HOLE {hole}/{count}   |   SHOTS {shots}"; }
        public void HideOverlay()
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            panel.gameObject.SetActive(false); restart.onClick.RemoveAllListeners();
        }
        public void ShowStart(Action onStart)
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            panel.gameObject.SetActive(true);
            group.alpha = 1f; panel.localScale = Vector3.one;
            title.text = "CANDY MINI GOLF";
            description.text = "3 HOLES\nHold SPACE to charge\nRelease SPACE to shoot";
            buttonLabel.text = "START";
            restart.gameObject.SetActive(true);
            restart.interactable = true;
            restart.onClick.RemoveAllListeners();
            restart.onClick.AddListener(() => onStart());
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(restart.gameObject);
        }
        public void ShowClear(int hole, int shots)
        {
            panel.gameObject.SetActive(true); restart.gameObject.SetActive(false);
            title.text = "HOLE CLEAR!"; description.text = $"HOLE {hole}\n{shots} shots";
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(PopIn());
        }
        private IEnumerator PopIn()
        {
            float elapsed = 0;
            while (elapsed < 0.25f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.25f);
                group.alpha = t; panel.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, t);
                yield return null;
            }
            group.alpha = 1f; panel.localScale = Vector3.one;
        }
        public void ShowResult(string details, Action onRestart)
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            panel.gameObject.SetActive(true); group.alpha = 1f; panel.localScale = Vector3.one;
            title.text = onRestart == null ? "SETUP ERROR" : "ALL HOLES CLEAR!";
            description.text = details;
            restart.onClick.RemoveAllListeners(); restart.interactable = true;
            buttonLabel.text = "BACK TO HOLE 1";
            restart.gameObject.SetActive(onRestart != null);
            if (onRestart != null)
            {
                restart.onClick.AddListener(() => onRestart());
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(restart.gameObject);
            }
        }
        public void DisableRestart() { restart.interactable = false; }
        private void OnDestroy() { if (canvasObject != null) Destroy(canvasObject); }
    }
}

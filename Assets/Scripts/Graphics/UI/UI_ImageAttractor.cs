using System;
using System.Collections.Generic;
using DG.Tweening;
using Extensions;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Graphics.UI
{
    public class UI_ImageAttractor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Canvas _canvas;

        [Header("Preferences")]
        [SerializeField] private GameObject _prefab;

        [Header("Animation Preferences")]
        [SerializeField] private float _duration;

        [Header("Scale Preferences")]
        [SerializeField] private Vector3 _startScale;
        [SerializeField] private Vector3 _endScale;
        [SerializeField] private AnimationCurve _scaleCurve;

        [Header("Move Preferences")]
        [SerializeField] private float _maxHookRange;
        [SerializeField] private AnimationCurve _moveCurve;

        [Header("Rotate Preferences")]
        [SerializeField] private Vector3 _startRotation;
        [SerializeField] private Vector3 _endRotation;
        [SerializeField] private AnimationCurve _rotateCurve;
        [SerializeField] private bool _randomizeStartRotation;

        [Header("Fade Preferences")]
        [SerializeField] private float _startAlpha;
        [SerializeField] private float _endAlpha;
        [SerializeField] private AnimationCurve _fadeCurve;

        [Header("Interval Preferences")]
        [SerializeField] private int _minIntervalCount;
        [SerializeField] private int _maxIntervalCount;
        [SerializeField] private float _minInterval;
        [SerializeField] private float _maxInterval;
        [SerializeField] private AnimationCurve _intervalCurve;

        [Header("Range Preferences")]
        [SerializeField] private int _minRangeCount;
        [SerializeField] private int _maxRangeCount;
        [SerializeField] private float _minRange;
        [SerializeField] private float _maxRange;
        [SerializeField] private AnimationCurve _rangeCurve;

        private HashSet<Sequence> _sequences = new HashSet<Sequence>();
        private CompositeDisposable _subscriptions = new CompositeDisposable();

        #region MonoBehaviour

        private void OnValidate()
        {
            _rectTransform ??= GetComponent<RectTransform>();
            _canvas ??= FindObjectOfType<Canvas>();
        }

        private void OnDestroy()
        {
            KillAll();
        }

        public void Play(int count, Vector3 fromWorld, Transform toWorld, Action onComplete = null, Action onAllCompleted = null)
            => Play(count, fromWorld, toWorld, EvaluateRange(count), onComplete, onAllCompleted);

        public void Play(int count, Vector3 fromWorld, Transform toWorld, float range, Action onComplete = null, Action onAllCompleted = null)
        {
            float delay = 0f;
            float interval = EvaluateInterval(count);

            for (int i = 0; i < count; i++)
            {
                int index = i;
                Observable.Timer(TimeSpan.FromSeconds(delay)).Subscribe(_ =>
                {
                    Play(fromWorld, toWorld, range, () =>
                    {
                        onComplete?.Invoke();

                        if (index == count - 1)
                        {
                            onAllCompleted?.Invoke();
                        }
                    });
                }).AddTo(_subscriptions);

                delay += interval;
            }
        }

        public void Play(Vector3 fromWorld, Transform toWorld, float range, Action onComplete = null)
        {
            GameObject instance = Instantiate(_prefab, fromWorld, Quaternion.identity);
            RectTransform rectTransform = instance.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = instance.AddComponent<CanvasGroup>();
            rectTransform.SetParent(_rectTransform);

            Vector3 centerAnchoredPosition = GetAnchoredPosition(fromWorld);

            Vector3 startAnchoredPosition = centerAnchoredPosition + Random.insideUnitSphere.normalized * range;

            Vector3 startRotation = Vector3.zero;

            if (_randomizeStartRotation)
            {
                startRotation.z = Random.Range(0f, 360f);
            }
            else
            {
                startRotation = _startRotation;
            }

            rectTransform.anchoredPosition3D = startAnchoredPosition;
            rectTransform.localScale = _startScale;
            rectTransform.localRotation = Quaternion.Euler(startRotation);
            canvasGroup.alpha = _startAlpha;

            Sequence sequence = DOTween.Sequence();

            sequence
                .Append(rectTransform.DOScale(_endScale, _duration).SetEase(_scaleCurve))
                .Join(canvasGroup.DOFade(_endAlpha, _duration).SetEase(_fadeCurve))
                .Join(rectTransform.DOLocalRotate(_endRotation, _duration).SetEase(_rotateCurve))
                .Join(CreateMoveTween(rectTransform, startAnchoredPosition, toWorld))
                .OnComplete(() =>
                {
                    Destroy(instance);
                    _sequences.Remove(sequence);
                    onComplete?.Invoke();
                })
                .Play();
        }

        #endregion

        private Tween CreateMoveTween(RectTransform rectTransform, Vector3 startAnchoredPosition, Transform toWorld)
        {
            float progress = 0f;
            Tween tween = DOTween
                .To(() => progress, x => progress = x, 1f, _duration)
                .OnUpdate(() =>
                {
                    Vector3 targetAnchoredPosition = GetAnchoredPosition(toWorld.position);
                    Vector3 anchoredPosition = CustomLerpFunctions.Slerp(startAnchoredPosition, targetAnchoredPosition, progress);
                    rectTransform.anchoredPosition3D = anchoredPosition;
                })
                .SetEase(_moveCurve);

            return tween;
        }

        private float EvaluateInterval(int count)
        {
            return _intervalCurve.Evaluate(_minIntervalCount, _maxIntervalCount, count, _minInterval, _maxInterval);
        }

        private float EvaluateRange(int count)
        {
            return _rangeCurve.Evaluate(_minRangeCount, _maxRangeCount, count, _minRange, _maxRange);
        }

        private void KillAll()
        {
            foreach (var sequence in _sequences)
            {
                sequence.Kill();

                if (sequence.target is GameObject target)
                {
                    Destroy(target);
                }
            }

            _sequences.Clear();

            _subscriptions.Clear();
        }

        private Vector3 GetAnchoredPosition(Vector3 worldPoint)
        {
            Camera camera = null;

            if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                camera = _canvas.worldCamera;
            }

            Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPoint);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPoint, camera, out Vector2 anchoredPosition);

            return anchoredPosition;
        }
    }
}

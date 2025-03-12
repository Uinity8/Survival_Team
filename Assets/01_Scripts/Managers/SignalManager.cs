using System;
using System.Collections.Generic;
using UnityEngine;

// SignalManager: Action과 Func의 혼합 방식을 적용한 유연한 신호 처리 시스템.
// - 고정된 매개변수 타입은 제너릭으로 처리
// - 유동적인 매개변수 개수/타입은 object[] 로 처리
namespace Managers
{
    public class SignalManager : Singleton<SignalManager>
    {
        // 신호 저장소 (Delegate 타입으로 Action과 Func 모두 저장 가능)
        private readonly Dictionary<string, Delegate> _signals = new();

        // ==================== Action ====================

        /// <summary>
        /// 고정된 매개변수를 사용하는 Action(T) 신호를 연결합니다.
        /// </summary>
        public void ConnectSignal<T>(string signalKey, Action<T> action)
        {
            if (!_signals.TryAdd(signalKey, action))
            {
                _signals[signalKey] = Delegate.Combine(_signals[signalKey], action);
            }
        }

        /// <summary>
        /// 고정된 매개변수 2개를 사용하는 Action(T1, T2) 신호를 연결합니다.
        /// </summary>
        public void ConnectSignal<T1, T2>(string signalKey, Action<T1, T2> action)
        {
            if (!_signals.TryAdd(signalKey, action))
            {
                _signals[signalKey] = Delegate.Combine(_signals[signalKey], action);
            }
        }

        /// <summary>
        /// 동적인 매개변수를 사용하는 Action(object[]) 신호를 연결합니다.
        /// </summary>
        public void ConnectSignal(string signalKey, Action<object[]> action)
        {
            if (!_signals.TryAdd(signalKey, action))
            {
                _signals[signalKey] = Delegate.Combine(_signals[signalKey], action);
            }
        }

        /// <summary>
        /// 고정된 매개변수를 사용하는 Action(T) 신호를 호출합니다.
        /// </summary>
        public void EmitSignal<T>(string signalKey, T arg)
        {
            if (!_signals.TryGetValue(signalKey, out var signal))
            {
                Debug.LogWarning($"SignalManager: Signal '{signalKey}' not found.");
                return;
            }

            if (signal is Action<T> action)
            {
                action.Invoke(arg);
            }
            else
            {
                Debug.LogError(
                    $"SignalManager: Signal '{signalKey}' has an incompatible delegate type. Expected: Action<{typeof(T)}>.");
            }
        }

        /// <summary>
        /// 고정된 매개변수 2개를 사용하는 Action(T1, T2) 신호를 호출합니다.
        /// </summary>
        public void EmitSignal<T1, T2>(string signalKey, T1 arg1, T2 arg2)
        {
            if (!_signals.TryGetValue(signalKey, out var signal))
            {
                Debug.LogWarning($"SignalManager: Signal '{signalKey}' not found.");
                return;
            }

            if (signal is Action<T1, T2> action)
            {
                action.Invoke(arg1, arg2);
            }
            else
            {
                Debug.LogError(
                    $"SignalManager: Signal '{signalKey}' has an incompatible delegate type. Expected: Action<{typeof(T1)}, {typeof(T2)}>.");
            }
        }

        /// <summary>
        /// 동적인 매개변수를 사용하는 Action(object[]) 신호를 호출합니다.
        /// </summary>
        public void EmitSignal(string signalKey, params object[] args)
        {
            if (!_signals.TryGetValue(signalKey, out var signal))
            {
                Debug.LogWarning($"SignalManager: Signal '{signalKey}' not found.");
                return;
            }

            if (signal is Action<object[]> action)
            {
                action.Invoke(args);
            }
            else
            {
                Debug.LogError(
                    $"SignalManager: Signal '{signalKey}' has an incompatible delegate type. Expected: Action<object[]>.");
            }
        }

        // ==================== Func ====================

        /// <summary>
        /// 고정된 매개변수를 사용하는 Func(T, TResult) 신호를 연결합니다.
        /// </summary>
        public void ConnectSignal<T, TResult>(string signalKey, Func<T, TResult> func)
        {
            if (!_signals.TryAdd(signalKey, func))
            {
                _signals[signalKey] = Delegate.Combine(_signals[signalKey], func);
            }
        }

        /// <summary>
        /// 고정된 매개변수 2개를 사용하는 Func(T1, T2, TResult) 신호를 연결합니다.
        /// </summary>
        public void ConnectSignal<T1, T2, TResult>(string signalKey, Func<T1, T2, TResult> func)
        {
            if (!_signals.TryAdd(signalKey, func))
            {
                _signals[signalKey] = Delegate.Combine(_signals[signalKey], func);
            }
        }

        /// <summary>
        /// 동적인 매개변수를 사용하는 Func(object[], TResult) 신호를 연결합니다.
        /// </summary>
        public void ConnectSignal<TResult>(string signalKey, Func<object[], TResult> func)
        {
            if (!_signals.TryAdd(signalKey, func))
            {
                _signals[signalKey] = Delegate.Combine(_signals[signalKey], func);
            }
        }

        /// <summary>
        /// 고정된 매개변수를 사용하는 Func(T, TResult) 신호를 호출합니다.
        /// </summary>
        public TResult EmitSignal<T, TResult>(string signalKey, T arg)
        {
            if (!_signals.TryGetValue(signalKey, out var signal))
            {
                Debug.LogWarning($"SignalManager: Signal '{signalKey}' not found.");
                return default;
            }

            if (signal is Func<T, TResult> func)
            {
                return func.Invoke(arg);
            }

            Debug.LogError(
                $"SignalManager: Signal '{signalKey}' has an incompatible delegate type. Expected: Func<{typeof(T)}, {typeof(TResult)}>.");
            return default;
        }

        /// <summary>
        /// 고정된 매개변수 2개를 사용하는 Func(T1, T2, TResult) 신호를 호출합니다.
        /// </summary>
        public TResult EmitSignal<T1, T2, TResult>(string signalKey, T1 arg1, T2 arg2)
        {
            if (!_signals.TryGetValue(signalKey, out var signal))
            {
                Debug.LogWarning($"SignalManager: Signal '{signalKey}' not found.");
                return default;
            }

            if (signal is Func<T1, T2, TResult> func)
            {
                return func.Invoke(arg1, arg2);
            }

            Debug.LogError(
                $"SignalManager: Signal '{signalKey}' has an incompatible delegate type. Expected: Func<{typeof(T1)}, {typeof(T2)}, {typeof(TResult)}>.");
            return default;
        }

        /// <summary>
        /// 동적인 매개변수를 사용하는 Func(object[], TResult) 신호를 호출합니다.
        /// </summary>
        public TResult EmitSignal<TResult>(string signalKey, params object[] args)
        {
            if (!_signals.TryGetValue(signalKey, out var signal))
            {
                Debug.LogWarning($"SignalManager: Signal '{signalKey}' not found.");
                return default;
            }

            if (signal is Func<object[], TResult> func)
            {
                return func.Invoke(args);
            }

            Debug.LogError(
                $"SignalManager: Signal '{signalKey}' has an incompatible delegate type. Expected: Func<object[], {typeof(TResult)}>.");
            return default;
        }

        // ==================== Debug ====================

        /// <summary>
        /// 현재 등록된 모든 신호를 디버깅용으로 출력합니다.
        /// </summary>
        public void DebugSignals()
        {
            Debug.Log("===== SignalManager Debug Info =====");
            if (_signals.Count == 0)
            {
                Debug.Log("No signals are currently registered.");
                return;
            }

            foreach (var signal in _signals)
            {
                var invocationList = signal.Value?.GetInvocationList();
                var listenerCount = invocationList?.Length ?? 0;
                Debug.Log(
                    $"Signal Key: {signal.Key}, Listener Count: {listenerCount}, Type: {signal.Value?.GetType()}");
            }

            Debug.Log("===================================");
        }
        
    }
}
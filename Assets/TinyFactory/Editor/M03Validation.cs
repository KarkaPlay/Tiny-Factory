#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Text;
using TinyFactory;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyFactory.Editor
{

internal static class M03Validation
    {
        private static readonly System.Collections.Generic.List<GameObject> TestHosts = new System.Collections.Generic.List<GameObject>();
        private static readonly System.Collections.Generic.List<UnityEngine.Object> TestAssets = new System.Collections.Generic.List<UnityEngine.Object>();
private static void SyntheticHarvestClick()
        {
            FactoryRuntime runtime = GameObject.Find("Tiny Factory Runtime")?.GetComponent<FactoryRuntime>();
            FactoryPresentation view = GameObject.Find("Tiny Factory Runtime")?.GetComponent<FactoryPresentation>();
            FieldInfo field = typeof(FactoryPresentation).GetField("harvestButton", BindingFlags.Instance | BindingFlags.NonPublic);
            Button button = view != null && field != null ? field.GetValue(view) as Button : null;
            if (runtime == null || button == null || EventSystem.current == null)
            {
                Debug.LogError("Synthetic UI click unavailable: Play Mode HUD/EventSystem is missing.");
                return;
            }
            InvokeLifecycle(runtime, "OnApplicationFocus", true);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Debug.Log($"M03 SYNTHETIC UI POINTER CLICK: foreground={runtime.IsForeground}, source buffer={runtime.SourceBuffer}, expected 1.");
            Require(runtime.SourceBuffer == 1, "synthetic harvest button click is accepted");
        }

        [MenuItem("Tiny Factory/Run M03 Deterministic Validation")]
        
private static void Run()
        {
            CleanupTestObjects();
            try
            {
                var output = new StringBuilder();
                ValidateTrace(output);
                ValidateThroughput(output);
                ValidateBuffersPausePurchasesAndCap(output);
                Debug.Log("M03 VALIDATION PASS\\n" + output);
            }
            catch (Exception exception)
            {
                Debug.LogError("M03 VALIDATION FAIL: " + exception);
            }
            finally
            {
                CleanupTestObjects();
            }
        }

private static void CleanupTestObjects()
        {
            for (int i = TestHosts.Count - 1; i >= 0; i--)
                if (TestHosts[i] != null) UnityEngine.Object.DestroyImmediate(TestHosts[i]);
            TestHosts.Clear();
            for (int i = TestAssets.Count - 1; i >= 0; i--)
                if (TestAssets[i] != null) UnityEngine.Object.DestroyImmediate(TestAssets[i]);
            TestAssets.Clear();
        }


        private static void ValidateTrace(StringBuilder output)
        {
            FactoryRuntime runtime = NewRuntime(null, out GameObject host);
            Require(runtime.RequestHarvest(), "t0 manual harvest accepted");
            long sold6 = 0, coins6 = 0, sold30 = 0, coins30 = 0, sold42 = 0, coins42 = 0, sold59 = 0, coins59 = 0;
            for (int t = 1; t <= 60; t++)
            {
                if (t == 42) Require(runtime.RequestSpeedUpgrade(), "speed request queued once at t42");
                runtime.Tick();
                if (t == 6) { sold6 = runtime.Sold; coins6 = runtime.Coins; }
                if (t == 30) { sold30 = runtime.Sold; coins30 = runtime.Coins; }
                if (t == 42) { sold42 = runtime.Sold; coins42 = runtime.Coins; }
                if (t == 59) { sold59 = runtime.Sold; coins59 = runtime.Coins; }
                if (t < 60 && t % 6 == 0) runtime.RequestHarvest();
            }
            Require(runtime.Sold == 14 && runtime.Coins == 20, $"60s trace expected 14/20, got {runtime.Sold}/{runtime.Coins}");
            Require(sold6 == 1 && coins6 == 4, $"first sale expected t6=1/4, got {sold6}/{coins6}");
            Require(sold30 == 6 && coins30 == 24, $"t30 expected 6/24, got {sold30}/{coins30}");
            Require(sold42 == 9 && coins42 == 0 && runtime.SpeedLevel == 1, $"t42 expected 9/0 + Speed L1, got {sold42}/{coins42}/L{runtime.SpeedLevel}");
            Require(sold59 == 14 && coins59 == 20, $"t59 expected 14/20, got {sold59}/{coins59}");
            output.AppendLine($"trace t6={sold6}/{coins6}, t30={sold30}/{coins30}, t42={sold42}/{coins42} Speed L1, t59={sold59}/{coins59}");
            UnityEngine.Object.DestroyImmediate(host);
        }

        private static void ValidateThroughput(StringBuilder output)
        {
            int baseline = SaturatedSales(0);
            int speedOne = SaturatedSales(1);
            Require(speedOne > baseline, $"Speed L1 throughput must improve ({baseline}→{speedOne}/120s)");
            output.AppendLine($"saturated 120s sales: L0={baseline}, Speed L1={speedOne}");
        }

        private static int SaturatedSales(int speed)
        {
            FactoryRuntime runtime = NewRuntime(null, out GameObject host);
            SetField(runtime, "speedLevel", speed);
            for (int t = 0; t < 120; t++)
            {
                if (runtime.SourceBuffer < runtime.Capacity) runtime.RequestHarvest();
                runtime.Tick();
            }
            int result = checked((int)runtime.Sold);
            UnityEngine.Object.DestroyImmediate(host);
            return result;
        }

        private static void ValidateBuffersPausePurchasesAndCap(StringBuilder output)
        {
            FactoryBalanceConfig noAuto = UnityEngine.Object.Instantiate(Resources.Load<FactoryBalanceConfig>("FactoryBalanceConfig"));
            TestAssets.Add(noAuto);

            noAuto.automationIntervalSeconds = 100000;
            noAuto.packagerSeconds = 20;
            FactoryRuntime runtime = NewRuntime(noAuto, out GameObject host);
            SetField(runtime, "secondsUntilAuto", noAuto.automationIntervalSeconds);
            int accepted = 0;
            bool sawBlockedWip = false;
            for (int i = 0; i < 500; i++)
            {
                if (runtime.RequestHarvest()) accepted++;
                runtime.Tick();
                if (runtime.DryerHasWip && runtime.DryerRemaining == 0 && runtime.PackagerInput == runtime.Capacity)
                    sawBlockedWip = true;
                Require(runtime.TotalInFlight <= 18, "maximum in-flight stays within 18 while buffers fill");
            }
            Require(sawBlockedWip, "completed dryer WIP waits when downstream input buffer is full");
            Require(!runtime.RequestHarvest(), "full source refuses manual tap");
            Require(runtime.RejectedHarvests > 0 && runtime.TotalInFlight <= 18, "full-buffer refusal is visible and bounded");
            runtime.SetForeground(false);
            int frozenTicks = runtime.Ticks;
            long frozenSales = runtime.Sold;
            SetField(runtime, "tickAccumulator", 0.375f);
            runtime.Tick();
            Require(runtime.Ticks == frozenTicks && runtime.Sold == frozenSales, "background tick is frozen");
            InvokeLifecycle(runtime, "OnApplicationPause", true);
            InvokeLifecycle(runtime, "OnApplicationFocus", true);
            Require(!runtime.IsForeground, "focus return does not resume while app remains paused");
            Require(Math.Abs((float)GetField(runtime, "tickAccumulator") - 0.375f) < 0.0001f, "foreground remainder is preserved while paused");
            InvokeLifecycle(runtime, "OnApplicationPause", false);
            Require(runtime.IsForeground, "resume re-enables foreground simulation");
            for (int i = 0; i < 1000; i++)
            {
                runtime.Tick();
                Require(runtime.TotalInFlight <= 18, "maximum in-flight stays within 18");
            }
            Require(runtime.Sold == accepted, $"accepted items are eventually sold once after backpressure ({accepted} accepted, {runtime.Sold} sold)");
            runtime.SetForeground(false);
            Require(!runtime.RequestHarvest() && !runtime.RequestSpeedUpgrade(), "inactive inputs rejected");
            runtime.SetForeground(true);
            SetField(runtime, "coins", 35L);
            Require(runtime.RequestSpeedUpgrade(), "purchase can queue before commit");
            Require(!runtime.RequestSpeedUpgrade(), "repeated tap cannot queue a second purchase");
            SetField(runtime, "dryerHasWip", true);
            SetField(runtime, "dryerRemaining", 3);
            runtime.Tick();
            Require(runtime.SpeedLevel == 0 && runtime.Coins == 35 && runtime.DryerRemaining == 2, "funds rechecked and in-flight timer unchanged");
            SetField(runtime, "coins", 36L);
            Require(runtime.RequestSpeedUpgrade(), "purchase retries after funds become available");
            runtime.Tick();
            Require(runtime.SpeedLevel == 1 && runtime.Coins == 0, "exact-funds purchase commits atomically");
            SetField(runtime, "coins", noAuto.valueCap - 1);
            SetField(runtime, "sold", noAuto.valueCap - 1);
            SetField(runtime, "packagerHasWip", true);
            SetField(runtime, "packagerRemaining", 1);
            runtime.Tick();
            Require(runtime.Coins <= noAuto.valueCap && runtime.Sold <= noAuto.valueCap, "wallet and sales saturate at 1e12 cap");
            Require(noAuto.DryerDuration(0) == 3 && noAuto.DryerDuration(1) == 2, "configured dryer durations 3s→2s");
            output.AppendLine($"backpressure accepted={accepted}, all drained exactly once; funds recheck/double tap/in-flight timer, pause, inactive input, exact purchase and 1e12 cap passed");
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(noAuto);
        }

private static FactoryRuntime NewRuntime(FactoryBalanceConfig config, out GameObject host)
        {
            host = new GameObject("M03 Validation Runtime");
            host.hideFlags = HideFlags.DontSave;
            TestHosts.Add(host);
            FactoryRuntime runtime = host.AddComponent<FactoryRuntime>();
            if (config == null) config = Resources.Load<FactoryBalanceConfig>("FactoryBalanceConfig");
            Require(config != null, "balance config asset is loadable");
            SetField(runtime, "config", config);
            SetField(runtime, "secondsUntilAuto", config.automationIntervalSeconds);
            return runtime;
        }

        private static void SetField(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Require(info != null, "field exists: " + field);
            info.SetValue(target, value);
        }

        private static object GetField(object target, string field)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Require(info != null, "field exists: " + field);
            return info.GetValue(target);
        }

        private static void InvokeLifecycle(object target, string method, bool argument)
        {
            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Require(info != null, "lifecycle method exists: " + method);
            info.Invoke(target, new object[] { argument });
        }

        private static void Require(bool condition, string detail)
        {
            if (!condition) throw new InvalidOperationException(detail);
        }
    }
}
#endif

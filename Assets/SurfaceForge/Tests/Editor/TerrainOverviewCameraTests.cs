using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SurfaceForge.Tests
{
    public class TerrainOverviewCameraTests
    {
        private GameObject root;
        private TerrainOverviewCamera camera;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private void Wheel(float delta, bool allowed = true) => typeof(TerrainOverviewCamera)
            .GetMethod("ApplyWheel", Private).Invoke(camera, new object[] { delta, allowed });
        private float Zoom => (float)typeof(TerrainOverviewCamera).GetField("zoom", Private).GetValue(camera);
        [SetUp] public void Setup() { root = new GameObject("Zoom Test", typeof(Camera)); camera = root.AddComponent<TerrainOverviewCamera>(); }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(root);

        [Test] public void InterruptedWheelRequiresNeutralFrameBeforeAcceptingNewInput()
        {
            Wheel(0); Wheel(120); float before = Zoom;
            Assert.That(before, Is.LessThan(1));
            Wheel(-120, false); Wheel(-120);
            Assert.That(Zoom, Is.EqualTo(before));
            Wheel(0); Wheel(-120);
            Assert.That(Zoom, Is.EqualTo(1).Within(.00001f));
        }

        [Test] public void ZoomIsBoundedAndDisableRestoresInitialView()
        {
            Wheel(0);
            for (int i = 0; i < 20; i++) Wheel(1200);
            Assert.That(Zoom, Is.EqualTo(.35f).Within(.00001f));
            for (int i = 0; i < 20; i++) Wheel(-1200);
            Assert.That(Zoom, Is.EqualTo(1.5f).Within(.00001f));
            // EditMode 不進入 PlayerLoop；直接呼叫生命週期回呼檢查其重設規則。
            typeof(TerrainOverviewCamera).GetMethod("OnDisable", Private).Invoke(camera, null);
            typeof(TerrainOverviewCamera).GetMethod("OnEnable", Private).Invoke(camera, null);
            Assert.That(Zoom, Is.EqualTo(1));
            Wheel(120); Assert.That(Zoom, Is.EqualTo(1), "重啟後不能沿用尚未歸零的滾輪");
        }
    }
}

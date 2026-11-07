using System.Collections;
using NUnit.Framework;
using SurfaceForge.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurfaceForge.Tests
{
    public class ContextMenuFadeTests
    {
        private GameObject host;
        private float oldScale;
        private ContextMenuFade fade;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return new EnterPlayMode();
            oldScale = Time.timeScale; Time.timeScale = 0;
            host = new GameObject("Fade Test");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            fade?.SnapClosed(); Object.Destroy(host); Time.timeScale = oldScale;
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator CloseRevokesInputAndReopenPreservesProgressWhilePaused()
        {
            var panel = new GameObject("Panel", typeof(RectTransform)); panel.transform.SetParent(host.transform);
            fade = new ContextMenuFade(); fade.Initialize(host, panel);
            var group = panel.GetComponent<CanvasGroup>();
            fade.SetVisible(true);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(group.alpha, Is.EqualTo(1).Within(.001f), "timeScale=0 仍完成淡入");
            fade.SetVisible(false);
            Assert.That(group.interactable || group.blocksRaycasts, Is.False);
            Assert.That(panel.activeSelf, Is.True, "淡出完成前保留畫面");
            yield return new WaitForSecondsRealtime(.03f);
            float alpha = group.alpha; fade.SetVisible(true);
            Assert.That(group.alpha, Is.EqualTo(alpha).Within(.001f), "反向不重設透明度");
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(panel.activeSelf && fade.IsOpen, Is.True);
            fade.SetVisible(false); fade.SnapClosed();
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(panel.activeSelf || fade.IsOpen || group.blocksRaycasts, Is.False, "清理後不被舊回呼重新開啟");
        }
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Shrunk.Tests
{
    /// <summary>The pause menu's second page: text size and screen shake, and the way back.</summary>
    public class OptionsTests
    {
        [TearDown]
        public void CleanUp()
        {
            GameSettings.Reset();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator More_options_has_text_size_and_screen_shake()
        {
            GameSettings.Reset();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var menu = PauseMenu.Instance;
            menu.Open();
            Assert.AreEqual(0, menu.Page);
            menu.Use(menu.RowNamed("More options"));
            Assert.AreEqual(1, menu.Page, "More options did not turn the page");

            var hud = Object.FindAnyObjectByType<Hud>().GetComponentInParent<CanvasScaler>();
            float before = hud.referenceResolution.y;
            menu.Use(menu.RowNamed("Text size"));
            Assert.AreEqual(1.15f, GameSettings.TextSize, 0.001f);
            Assert.Less(hud.referenceResolution.y, before, "larger text did not make the writing bigger");
            menu.Use(menu.RowNamed("Text size"));
            Assert.AreEqual(0.85f, GameSettings.TextSize, 0.001f);
            Assert.Greater(hud.referenceResolution.y, before);

            menu.Use(menu.RowNamed("Screen shake"));
            Assert.IsFalse(GameSettings.ScreenShake);
            menu.Use(menu.RowNamed("Back"));
            Assert.AreEqual(0, menu.Page);
            menu.Close();
        }
    }
}

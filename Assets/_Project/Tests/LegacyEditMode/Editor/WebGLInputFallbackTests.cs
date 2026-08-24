using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HomeProtector.Tests.LegacyEditMode
{
    public sealed class WebGLInputFallbackTests
    {
        [Test]
        public void MicrophoneSystemDefinesKeyboardFallbackAndSharedActivationRequest()
        {
            const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo fallbackEnabled = typeof(MicrophoneSystem).GetField(
                "keyboardFallbackEnabled",
                privateInstance);
            FieldInfo activationKey = typeof(MicrophoneSystem).GetField(
                "keyboardActivationKey",
                privateInstance);
            MethodInfo activationRequest = typeof(MicrophoneSystem).GetMethod(
                "RequestPlayerActivation",
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(fallbackEnabled, Is.Not.Null);
            Assert.That(fallbackEnabled.FieldType, Is.EqualTo(typeof(bool)));
            Assert.That(activationKey, Is.Not.Null);
            Assert.That(activationKey.FieldType, Is.EqualTo(typeof(KeyCode)));
            Assert.That(activationRequest, Is.Not.Null);
            Assert.That(activationRequest.ReturnType, Is.EqualTo(typeof(void)));
        }
    }
}

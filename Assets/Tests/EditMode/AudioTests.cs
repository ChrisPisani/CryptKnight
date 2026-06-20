using CryptKnight.Audio;
using NUnit.Framework;

namespace CryptKnight.Tests.EditMode
{
    public sealed class AudioTests
    {
        [Test]
        public void VolumeValuesStayInRange()
        {
            Assert.That(GameAudioSettings.ClampVolume(-0.5f), Is.EqualTo(0f));
            Assert.That(GameAudioSettings.ClampVolume(0.65f), Is.EqualTo(0.65f));
            Assert.That(GameAudioSettings.ClampVolume(1.5f), Is.EqualTo(1f));
        }
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nyangsta.Arcade;

namespace Nyangsta.Tests
{
    public class ArcadeProgressServiceTests
    {
        [Test]
        public void Constructor_NullList_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ArcadeProgressService(null));
        }

        [Test]
        public void MarkComplete_NewZone_RecordsAndRaisesEvent()
        {
            var backing = new List<string>();
            var service = new ArcadeProgressService(backing);
            string raised = null;
            service.ZoneCompleted += id => raised = id;

            bool result = service.MarkComplete("BuildZone_Table2");

            Assert.IsTrue(result);
            Assert.IsTrue(service.IsComplete("BuildZone_Table2"));
            Assert.AreEqual("BuildZone_Table2", raised);
            CollectionAssert.Contains(backing, "BuildZone_Table2");
        }

        [Test]
        public void MarkComplete_Duplicate_ReturnsFalseAndRaisesOnce()
        {
            var service = new ArcadeProgressService(new List<string>());
            int raiseCount = 0;
            service.ZoneCompleted += _ => raiseCount++;

            service.MarkComplete("HireZone_GrilledFish");
            bool second = service.MarkComplete("HireZone_GrilledFish");

            Assert.IsFalse(second);
            Assert.AreEqual(1, raiseCount);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void MarkComplete_InvalidId_ReturnsFalse(string id)
        {
            var backing = new List<string>();
            var service = new ArcadeProgressService(backing);

            Assert.IsFalse(service.MarkComplete(id));
            Assert.IsEmpty(backing);
        }

        [Test]
        public void IsComplete_UnknownOrInvalidId_ReturnsFalse()
        {
            var service = new ArcadeProgressService(new List<string> { "BuildZone_Table2" });

            Assert.IsFalse(service.IsComplete("BuildZone_Table3"));
            Assert.IsFalse(service.IsComplete(null));
            Assert.IsFalse(service.IsComplete(""));
        }

        [Test]
        public void IsComplete_PreloadedList_ReflectsSavedState()
        {
            // Simulates loading a save file where zones were already unlocked.
            var saved = new List<string> { "BuildZone_BerryLine", "HireZone_BerryJuice" };
            var service = new ArcadeProgressService(saved);

            Assert.IsTrue(service.IsComplete("BuildZone_BerryLine"));
            Assert.IsTrue(service.IsComplete("HireZone_BerryJuice"));
        }
    }
}

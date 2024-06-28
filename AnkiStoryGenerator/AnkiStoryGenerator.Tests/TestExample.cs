namespace AnkiStoryGenerator.Tests
{
    [TestClass]
    public class TestExample
    {
        [TestMethod]
        public void FailingTest()
        {
            Assert.Fail("Test: this test should intentionally fail and I should receive a notification email");
        }
    }
}
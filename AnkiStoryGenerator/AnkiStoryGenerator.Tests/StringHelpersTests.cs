using AnkiStoryGenerator.Utilities;

namespace AnkiStoryGenerator.Tests;

[TestClass]
public class StringHelpersTests
{
    [TestMethod]
    public void TrimBackticksWrapperFromString_ShouldRemoveBackticksAndContentType_WhenPresent()
    {
        // Arrange
        var input = @"```html
This is a code block
```";

        // Act
        var result = StringHelpers.RemoveBackticksBlockWrapper(input);

        // Assert
        Assert.AreEqual("This is a code block", result);
    }

    [TestMethod]
    public void TrimBackticksWrapperFromString_ShouldRemoveBackticksAndContentType_OnlyAtTheEnd()
    {
        // Arrange
        var input = @"```html
This is a code block
```
Some other text
```";

        // Act
        var result = StringHelpers.RemoveBackticksBlockWrapper(input);

        // Assert
        Assert.AreEqual(@"This is a code block
```
Some other text", result);
    }


    [TestMethod]
    public void TrimBackticksWrapperFromString_ShouldNotRemoveAnything_WhenBackticksAndContentTypeNotPresent()
    {
        // Arrange
        var input = "This is a normal string";

        // Act
        var result = StringHelpers.RemoveBackticksBlockWrapper(input);

        // Assert
        Assert.AreEqual(input, result);
    }
}

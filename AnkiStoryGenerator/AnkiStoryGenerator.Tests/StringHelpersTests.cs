using AnkiStoryGenerator.Services;

namespace AnkiStoryGenerator.Tests;

[TestClass]
public class StringHelpersTests
{
    [TestMethod]
    public void TrimBackticksWrapperFromString_ShouldRemoveBackticksAndContentType_WhenPresent()
    {
        // Arrange
        string input = @"```html
This is a code block
```";

        // Act
        string result = StringHelpers.TrimBackticksWrapperFromString(input);

        // Assert
        Assert.AreEqual("This is a code block", result);
    }

    [TestMethod]
    public void TrimBackticksWrapperFromString_ShouldRemoveBackticksAndContentType_OnlyAtTheEnd()
    {
        // Arrange
        string input = @"```html
This is a code block
```
Some other text
```";

        // Act
        string result = StringHelpers.TrimBackticksWrapperFromString(input);

        // Assert
        Assert.AreEqual(@"This is a code block
```
Some other text", result);
    }


    [TestMethod]
    public void TrimBackticksWrapperFromString_ShouldNotRemoveAnything_WhenBackticksAndContentTypeNotPresent()
    {
        // Arrange
        string input = "This is a normal string";

        // Act
        string result = StringHelpers.TrimBackticksWrapperFromString(input);

        // Assert
        Assert.AreEqual(input, result);
    }

}
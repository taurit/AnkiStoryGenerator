using AnkiStoryGenerator.Utilities;
using FluentAssertions;

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
        result.Should().Be("This is a code block");
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
        result.Should().Be(@"This is a code block
```
Some other text");
    }


    [TestMethod]
    public void TrimBackticksWrapperFromString_ShouldNotRemoveAnything_WhenBackticksAndContentTypeNotPresent()
    {
        // Arrange
        var input = "This is a normal string";

        // Act
        var result = StringHelpers.RemoveBackticksBlockWrapper(input);

        // Assert
        result.Should().Be(input);
    }
}

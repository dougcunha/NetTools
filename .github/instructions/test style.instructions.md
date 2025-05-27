---
applyTo: '**/*.cs'
---
Formatting and style standards for this UnitTests

- Use xUnit, Shouldly and NSubstitute.
- Place ExcludeFromCodeCoverage attribute on all test classes.
- All test classes must be public sealed.
- Use AAA (Arrange, Act, Assert) pattern.
- Use Method_Condition_ExpectedResult naming convention for test methods.
- Declare all dependencies as readonly fields and initialize it inline.
- Declare string const as const and use UPPER_CASE for names
- Use raw string to declare multi-line strings